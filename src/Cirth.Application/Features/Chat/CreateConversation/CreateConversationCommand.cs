using Cirth.Application.Common.Ports;
using Cirth.Domain.Conversations;
using Cirth.Shared;
using MediatR;

namespace Cirth.Application.Features.Chat.CreateConversation;

public sealed record CreateConversationCommand(string? Title = null) : IRequest<Result<ConversationDto>>;

public sealed record ConversationDto(Guid Id, string? Title, DateTimeOffset CreatedAt);

internal sealed class CreateConversationCommandHandler(
    ITenantProvider tenantProvider,
    IConversationRepository repo,
    IUnitOfWork uow) : IRequestHandler<CreateConversationCommand, Result<ConversationDto>>
{
    public async Task<Result<ConversationDto>> Handle(CreateConversationCommand cmd, CancellationToken ct)
    {
        var conv = Conversation.Create(tenantProvider.CurrentTenantId, tenantProvider.CurrentUserId, cmd.Title);
        await repo.AddAsync(conv, ct);
        await uow.CommitAsync(ct);
        return Result<ConversationDto>.Success(new(conv.Id.Value, conv.Title, conv.CreatedAt));
    }
}
