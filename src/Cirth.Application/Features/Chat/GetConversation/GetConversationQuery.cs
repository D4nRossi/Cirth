using Cirth.Application.Common.Ports;
using Cirth.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cirth.Application.Features.Chat.GetConversation;

public sealed record GetConversationQuery(Guid ConversationId) : IRequest<Result<ConversationDetailDto>>;

public sealed record ConversationDetailDto(Guid Id, string? Title, IReadOnlyList<MessageDto> Messages);

public sealed record MessageDto(Guid Id, string Role, string Content, Guid[] CitedChunkIds, DateTimeOffset CreatedAt);

internal sealed class GetConversationQueryHandler(
    ITenantProvider tenantProvider,
    IQueryDbContext db) : IRequestHandler<GetConversationQuery, Result<ConversationDetailDto>>
{
    public async Task<Result<ConversationDetailDto>> Handle(GetConversationQuery q, CancellationToken ct)
    {
        var tenantId = tenantProvider.CurrentTenantId.Value;
        var userId = tenantProvider.CurrentUserId.Value;

        var conv = await db.Conversations
            .Where(c => c.Id.Value == q.ConversationId
                && c.TenantId.Value == tenantId
                && c.UserId.Value == userId)
            .FirstOrDefaultAsync(ct);

        if (conv is null)
            return Error.NotFound("conversation.not_found", "Conversation not found.");

        var messages = await db.Messages
            .Where(m => m.ConversationId.Value == q.ConversationId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new MessageDto(m.Id.Value, m.Role.ToString(), m.Content, m.CitedChunkIds, m.CreatedAt))
            .ToListAsync(ct);

        return Result<ConversationDetailDto>.Success(new(conv.Id.Value, conv.Title, messages));
    }
}
