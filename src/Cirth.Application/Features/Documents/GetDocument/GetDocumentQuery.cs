using Cirth.Application.Common.Ports;
using Cirth.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cirth.Application.Features.Documents.GetDocument;

public sealed record GetDocumentQuery(Guid DocumentId) : IRequest<Result<DocumentDetailDto>>;

public sealed record DocumentDetailDto(
    Guid Id, string Title, string SourceType, string Status, string? Author,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    IReadOnlyList<DocumentVersionDto> Versions,
    IReadOnlyList<ListDocuments.TagDto> Tags,
    IReadOnlyList<CollectionRefDto> Collections);

public sealed record DocumentVersionDto(Guid Id, int VersionNumber, string ContentHash,
    long SizeBytes, string MimeType, bool IsCurrent, DateTimeOffset CreatedAt);

public sealed record CollectionRefDto(Guid Id, string Name);

internal sealed class GetDocumentQueryHandler(
    ITenantProvider tenantProvider,
    IQueryDbContext db) : IRequestHandler<GetDocumentQuery, Result<DocumentDetailDto>>
{
    public async Task<Result<DocumentDetailDto>> Handle(GetDocumentQuery q, CancellationToken ct)
    {
        var tenantId = tenantProvider.CurrentTenantId.Value;

        var doc = await db.Documents
            .Where(d => d.Id.Value == q.DocumentId && d.TenantId.Value == tenantId)
            .FirstOrDefaultAsync(ct);

        if (doc is null)
            return Error.NotFound("document.not_found", "Document not found.");

        var versions = await db.DocumentVersions
            .Where(v => v.DocumentId.Value == q.DocumentId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new DocumentVersionDto(v.Id.Value, v.VersionNumber, v.ContentHash,
                v.SizeBytes, v.MimeType, v.IsCurrent, v.CreatedAt))
            .ToListAsync(ct);

        var tags = await db.DocumentTags
            .Where(dt => dt.DocumentId == q.DocumentId)
            .Join(db.Tags, dt => dt.TagId, t => t.Id.Value, (_, t) => new ListDocuments.TagDto(t.Id.Value, t.Name, t.Color))
            .ToListAsync(ct);

        var collections = await db.CollectionDocuments
            .Where(cd => cd.DocumentId == q.DocumentId)
            .Join(db.Collections, cd => cd.CollectionId, c => c.Id.Value, (_, c) => new CollectionRefDto(c.Id.Value, c.Name))
            .ToListAsync(ct);

        return Result<DocumentDetailDto>.Success(new(
            doc.Id.Value, doc.Title, doc.SourceType.ToString(), doc.Status.ToString(), doc.Author,
            doc.CreatedAt, doc.UpdatedAt, versions, tags, collections));
    }
}
