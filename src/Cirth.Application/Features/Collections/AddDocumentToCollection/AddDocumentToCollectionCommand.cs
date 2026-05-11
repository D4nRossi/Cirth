using Cirth.Application.Common.Ports;
using Cirth.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cirth.Application.Features.Collections.AddDocumentToCollection;

public sealed record AddDocumentToCollectionCommand(Guid CollectionId, Guid DocumentId) : IRequest<Result<Unit>>;

internal sealed class AddDocumentToCollectionCommandHandler(
    ITenantProvider tenantProvider,
    IQueryDbContext db,
    IDocumentRelationsRepository relations,
    IUnitOfWork uow) : IRequestHandler<AddDocumentToCollectionCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(AddDocumentToCollectionCommand cmd, CancellationToken ct)
    {
        var tenantId = tenantProvider.CurrentTenantId.Value;

        var collectionExists = await db.Collections
            .AnyAsync(c => c.Id.Value == cmd.CollectionId && c.TenantId.Value == tenantId, ct);
        if (!collectionExists)
            return Error.NotFound("collection.not_found", "Collection not found.");

        var docExists = await db.Documents
            .AnyAsync(d => d.Id.Value == cmd.DocumentId && d.TenantId.Value == tenantId, ct);
        if (!docExists)
            return Error.NotFound("document.not_found", "Document not found.");

        var already = await db.CollectionDocuments
            .AnyAsync(cd => cd.CollectionId == cmd.CollectionId && cd.DocumentId == cmd.DocumentId, ct);
        if (already)
            return Result<Unit>.Success(Unit.Value);

        await relations.AddToCollectionAsync(cmd.CollectionId, cmd.DocumentId, ct);
        await uow.CommitAsync(ct);
        return Result<Unit>.Success(Unit.Value);
    }
}
