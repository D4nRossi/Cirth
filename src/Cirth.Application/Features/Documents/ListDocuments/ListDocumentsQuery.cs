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
    ITenantProvider tenantProvider,
    IQueryDbContext db) : IRequestHandler<ListDocumentsQuery, Result<PagedResult<DocumentSummaryDto>>>
{
    public async Task<Result<PagedResult<DocumentSummaryDto>>> Handle(ListDocumentsQuery q, CancellationToken ct)
    {
        var tenantId = tenantProvider.CurrentTenantId.Value;

        var query = db.Documents
            .Where(d => d.TenantId.Value == tenantId);

        if (q.Status.HasValue)
            query = query.Where(d => d.Status == q.Status.Value);

        if (q.CollectionId.HasValue)
            query = query.Where(d => db.CollectionDocuments.Any(cd =>
                cd.CollectionId == q.CollectionId.Value && cd.DocumentId == d.Id.Value));

        if (q.TagId.HasValue)
            query = query.Where(d => db.DocumentTags.Any(dt =>
                dt.DocumentId == d.Id.Value && dt.TagId == q.TagId.Value));

        var total = await query.CountAsync(ct);

        var documents = await query
            .OrderByDescending(d => d.UpdatedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync(ct);

        var docIds = documents.Select(d => d.Id.Value).ToList();
        var tags = await db.DocumentTags
            .Where(dt => docIds.Contains(dt.DocumentId))
            .Join(db.Tags, dt => dt.TagId, t => t.Id.Value, (dt, t) => new { dt.DocumentId, t })
            .ToListAsync(ct);

        var versionCounts = await db.DocumentVersions
            .Where(v => docIds.Contains(v.DocumentId.Value))
            .GroupBy(v => v.DocumentId.Value)
            .Select(g => new { DocId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var tagsByDoc = tags.GroupBy(x => x.DocumentId)
            .ToDictionary(g => g.Key, g => g.Select(x => new TagDto(x.t.Id.Value, x.t.Name, x.t.Color)).ToList());

        var countsByDoc = versionCounts.ToDictionary(x => x.DocId, x => x.Count);

        var items = documents.Select(d => new DocumentSummaryDto(
            d.Id.Value, d.Title, d.SourceType.ToString(), d.Status.ToString(), d.Author,
            countsByDoc.GetValueOrDefault(d.Id.Value, 0),
            d.CreatedAt, d.UpdatedAt,
            tagsByDoc.GetValueOrDefault(d.Id.Value) ?? [])).ToList();

        return Result<PagedResult<DocumentSummaryDto>>.Success(new(items, total, q.Page, q.PageSize));
    }
}
