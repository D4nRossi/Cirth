using Cirth.Application.Common.Ports;
using Cirth.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cirth.Application.Features.Tags.AddTagToDocument;

public sealed record AddTagToDocumentCommand(Guid DocumentId, Guid TagId) : IRequest<Result<Unit>>;

internal sealed class AddTagToDocumentCommandHandler(
    IQueryDbContext db,
    IDocumentRelationsRepository relations,
    IUnitOfWork uow) : IRequestHandler<AddTagToDocumentCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(AddTagToDocumentCommand cmd, CancellationToken ct)
    {
        var docId = new DocumentId(cmd.DocumentId);
        var tagId = new TagId(cmd.TagId);

        var docExists = await db.Documents.AnyAsync(d => d.Id == docId, ct);
        if (!docExists)
            return Error.NotFound("document.not_found", "Document not found.");

        var tagExists = await db.Tags.AnyAsync(t => t.Id == tagId, ct);
        if (!tagExists)
            return Error.NotFound("tag.not_found", "Tag not found.");

        var already = await db.DocumentTags
            .AnyAsync(dt => dt.DocumentId == docId && dt.TagId == tagId, ct);
        if (already)
            return Result<Unit>.Success(Unit.Value);

        await relations.AddTagAsync(cmd.DocumentId, cmd.TagId, ct);
        await uow.CommitAsync(ct);
        return Result<Unit>.Success(Unit.Value);
    }
}
