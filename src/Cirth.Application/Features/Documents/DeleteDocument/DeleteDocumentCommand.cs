using Cirth.Application.Common.Ports;
using Cirth.Shared;
using FluentValidation;
using MediatR;

namespace Cirth.Application.Features.Documents.DeleteDocument;

public sealed record DeleteDocumentCommand(Guid DocumentId) : IRequest<Result<Unit>>;

internal sealed class DeleteDocumentCommandHandler(
    ITenantProvider tenantProvider,
    IDocumentRepository documentRepository,
    IChunkRepository chunkRepository,
    IVectorStore vectorStore,
    IObjectStorage objectStorage,
    IUnitOfWork uow) : IRequestHandler<DeleteDocumentCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(DeleteDocumentCommand cmd, CancellationToken ct)
    {
        var tenantId = tenantProvider.CurrentTenantId;
        var doc = await documentRepository.GetByIdAsync(new(cmd.DocumentId), ct);

        if (doc is null || doc.TenantId != tenantId)
            return Error.NotFound("document.not_found", "Document not found.");

        foreach (var version in doc.Versions)
        {
            var chunks = await chunkRepository.GetByVersionAsync(version.Id, ct);
            var qdrantIds = chunks.Select(c => c.Id).ToList();
            if (qdrantIds.Count > 0)
                await vectorStore.DeleteAsync(tenantId, qdrantIds, ct);

            await objectStorage.DeleteAsync("cirth-uploads", version.StorageKey, ct);
        }

        documentRepository.Remove(doc);
        await uow.CommitAsync(ct);

        return Result<Unit>.Success(Unit.Value);
    }
}
