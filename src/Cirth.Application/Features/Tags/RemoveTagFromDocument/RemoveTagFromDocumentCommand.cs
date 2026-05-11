using Cirth.Application.Common.Ports;
using Cirth.Shared;
using MediatR;

namespace Cirth.Application.Features.Tags.RemoveTagFromDocument;

public sealed record RemoveTagFromDocumentCommand(Guid DocumentId, Guid TagId) : IRequest<Result<Unit>>;

internal sealed class RemoveTagFromDocumentCommandHandler(
    IDocumentRelationsRepository relations,
    IUnitOfWork uow) : IRequestHandler<RemoveTagFromDocumentCommand, Result<Unit>>
{
    public async Task<Result<Unit>> Handle(RemoveTagFromDocumentCommand cmd, CancellationToken ct)
    {
        await relations.RemoveTagAsync(cmd.DocumentId, cmd.TagId, ct);
        await uow.CommitAsync(ct);
        return Result<Unit>.Success(Unit.Value);
    }
}
