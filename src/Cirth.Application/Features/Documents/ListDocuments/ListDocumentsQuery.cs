using Cirth.Application.Common.Ports;
using Cirth.Domain.Documents;
using Cirth.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cirth.Application.Features.Documents.ListDocuments;

public sealed record ListDocumentsQuery(
    Guid? CollectionId = null,
    Guid? TagId = null,
    DocumentStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<DocumentSummaryDto>>>;

public sealed record DocumentSummaryDto(
    Guid Id, string Title, string SourceType, string Status,
    string? Author, int VersionCount, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    IReadOnlyList<TagDto> Tags);

public sealed record TagDto(Guid Id, string Name, string? Color);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

internal sealed class ListDocumentsQueryHandler(
    IQueryDbContext db) : IRequestHandler<ListDocumentsQuery, Result<PagedResult<DocumentSummaryDto>>>
{
    public async Task<Result<PagedResult<DocumentSummaryDto>>> Handle(ListDocumentsQuery q, CancellationToken ct)
    {
        var query = db.Documents.AsQueryable();

        if (q.Status.HasValue)
            query = query.Where(d => d.Status == q.Status.Value);

        if (q.CollectionId.HasValue)
        {
            var colId = new CollectionId(q.CollectionId.Value);
            query = query.Where(d => db.CollectionDocuments.Any(cd => cd.CollectionId == colId && cd.DocumentId == d.Id));
        }

        if (q.TagId.HasValue)
        {
            var tagId = new TagId(q.TagId.Value);
            query = query.Where(d => db.DocumentTags.Any(dt => dt.DocumentId == d.Id && dt.TagId == tagId));
        }

        var total = await query.CountAsync(ct);

        var documents = await query
            .OrderByDescending(d => d.UpdatedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        var docIds = documents.Select(d => d.Id).ToList();

        var tags = await db.DocumentTags
            .Where(dt => docIds.Contains(dt.DocumentId))
            .Join(db.Tags, dt => dt.TagId, t => t.Id, (dt, t) => new { dt.DocumentId, t })
            .ToListAsync(ct);

        var versionCounts = await db.DocumentVersions
            .Where(v => docIds.Contains(v.DocumentId))
            .GroupBy(v => v.DocumentId)
            .Select(g => new { DocId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var tagsByDoc = tags
            .GroupBy(x => x.DocumentId)
            .ToDictionary(g => g.Key, g => g.Select(x => new TagDto(x.t.Id.Value, x.t.Name, x.t.Color)).ToList());

        var countsByDoc = versionCounts.ToDictionary(x => x.DocId, x => x.Count);

        var items = documents.Select(d => new DocumentSummaryDto(
            d.Id.Value, d.Title, d.SourceType.ToString(), d.Status.ToString(), d.Author,
            countsByDoc.GetValueOrDefault(d.Id, 0),
            d.CreatedAt, d.UpdatedAt,
            tagsByDoc.GetValueOrDefault(d.Id) ?? [])).ToList();

        return Result<PagedResult<DocumentSummaryDto>>.Success(new(items, total, q.Page, q.PageSize));
    }
}
