using Cirth.Application.Common.Ports;
using Cirth.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cirth.Application.Features.Search.HybridSearch;

public sealed record HybridSearchQuery(
    string Query,
    int TopK = 8,
    Guid? CollectionId = null,
    Guid? TagId = null,
    string? Author = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null) : IRequest<Result<HybridSearchResult>>;

public sealed record HybridSearchResult(IReadOnlyList<SearchHit> Hits, string Query, TimeSpan Elapsed);

public sealed record SearchHit(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentTitle,
    string? Author,
    int Ordinal,
    string Content,
    string? Highlight,
    double Score,
    IReadOnlyList<string> Tags);

public sealed class HybridSearchQueryValidator : AbstractValidator<HybridSearchQuery>
{
    public HybridSearchQueryValidator()
    {
        RuleFor(x => x.Query).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TopK).InclusiveBetween(1, 50);
    }
}

internal sealed class HybridSearchQueryHandler(
    ITenantProvider tenantProvider,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    IBm25SearchService bm25,
    IQueryDbContext db) : IRequestHandler<HybridSearchQuery, Result<HybridSearchResult>>
{
    private const int RrfK = 60;
    private const int FetchSize = 50;

    public async Task<Result<HybridSearchResult>> Handle(HybridSearchQuery q, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var tenantId = tenantProvider.CurrentTenantId;

        var embeddingTask = embeddingService.EmbedAsync(q.Query, ct);
        var bm25Task = bm25.SearchChunksAsync(tenantId.Value, q.Query, FetchSize, ct);

        await Task.WhenAll(embeddingTask, bm25Task);

        var embedding = await embeddingTask;
        var bm25Ids = await bm25Task;
        var vectorHits = await vectorStore.SearchAsync(tenantId, embedding, FetchSize, ct);

        var scores = new Dictionary<Guid, double>();
        RankMerge(bm25Ids, scores);
        RankMerge(vectorHits.Select(h => h.ChunkId.Value).ToList(), scores);

        var topIds = scores.OrderByDescending(kv => kv.Value)
            .Take(q.TopK)
            .Select(kv => kv.Key)
            .ToList();

        if (topIds.Count == 0)
        {
            sw.Stop();
            return Result<HybridSearchResult>.Success(new([], q.Query, sw.Elapsed));
        }

        var chunks = await db.Chunks
            .Where(c => topIds.Contains(c.Id.Value) && c.IsCurrent)
            .ToListAsync(ct);

        var versionIds = chunks.Select(c => c.DocumentVersionId.Value).Distinct().ToList();
        var versions = await db.DocumentVersions
            .Where(v => versionIds.Contains(v.Id.Value))
            .ToListAsync(ct);

        var documentIds = versions.Select(v => v.DocumentId.Value).Distinct().ToList();

        var docQuery = db.Documents.Where(d => documentIds.Contains(d.Id.Value));
        if (!string.IsNullOrWhiteSpace(q.Author))
            docQuery = docQuery.Where(d => d.Author != null && d.Author.Contains(q.Author));
        if (q.DateFrom.HasValue)
            docQuery = docQuery.Where(d => d.CreatedAt >= q.DateFrom.Value);
        if (q.DateTo.HasValue)
            docQuery = docQuery.Where(d => d.CreatedAt <= q.DateTo.Value);
        if (q.CollectionId.HasValue)
            docQuery = docQuery.Where(d => db.CollectionDocuments.Any(cd =>
                cd.CollectionId == q.CollectionId.Value && cd.DocumentId == d.Id.Value));
        if (q.TagId.HasValue)
            docQuery = docQuery.Where(d => db.DocumentTags.Any(dt =>
                dt.DocumentId == d.Id.Value && dt.TagId == q.TagId.Value));

        var documents = await docQuery.ToListAsync(ct);
        var filteredDocIds = documents.Select(d => d.Id.Value).ToHashSet();

        var tagsJoin = await db.DocumentTags
            .Where(dt => filteredDocIds.Contains(dt.DocumentId))
            .Join(db.Tags, dt => dt.TagId, t => t.Id.Value, (dt, t) => new { dt.DocumentId, t.Name })
            .ToListAsync(ct);

        var tagsByDoc = tagsJoin.GroupBy(x => x.DocumentId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        var versionMap = versions.ToDictionary(v => v.Id.Value);
        var docMap = documents.ToDictionary(d => d.Id.Value);

        var hits = chunks
            .Select(c =>
            {
                var version = versionMap.GetValueOrDefault(c.DocumentVersionId.Value);
                if (version is null) return null;
                var doc = docMap.GetValueOrDefault(version.DocumentId.Value);
                if (doc is null) return null;
                var docId = doc.Id.Value;
                return new SearchHit(
                    c.Id.Value, docId, doc.Title, doc.Author,
                    c.Ordinal, c.Content, null,
                    scores.GetValueOrDefault(c.Id.Value, 0),
                    tagsByDoc.GetValueOrDefault(docId) ?? []);
            })
            .Where(h => h is not null)
            .Cast<SearchHit>()
            .OrderByDescending(h => h.Score)
            .ToList();

        sw.Stop();
        return Result<HybridSearchResult>.Success(new(hits, q.Query, sw.Elapsed));
    }

    private static void RankMerge(IReadOnlyList<Guid> orderedIds, Dictionary<Guid, double> scores)
    {
        for (int i = 0; i < orderedIds.Count; i++)
        {
            var score = 1.0 / (RrfK + i + 1);
            scores[orderedIds[i]] = scores.GetValueOrDefault(orderedIds[i], 0) + score;
        }
    }
}
