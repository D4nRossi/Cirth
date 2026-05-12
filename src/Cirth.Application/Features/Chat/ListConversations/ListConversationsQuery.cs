using Cirth.Application.Common.Ports;
using Cirth.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cirth.Application.Features.Chat.ListConversations;

public sealed record ListConversationsQuery : IRequest<Result<IReadOnlyList<ConversationSummaryDto>>>;

public sealed record ConversationSummaryDto(Guid Id, string? Title, int MessageCount, DateTimeOffset UpdatedAt);

internal sealed class ListConversationsQueryHandler(
    ITenantProvider tenantProvider,
    IQueryDbContext db) : IRequestHandler<ListConversationsQuery, Result<IReadOnlyList<ConversationSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<ConversationSummaryDto>>> Handle(ListConversationsQuery q, CancellationToken ct)
    {
        var userId = tenantProvider.CurrentUserId;

        var convs = await db.Conversations
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new ConversationSummaryDto(
                c.Id.Value, c.Title,
                db.Messages.Count(m => m.ConversationId == c.Id),
                c.UpdatedAt))
            .ToListAsync(ct);

        return Result<IReadOnlyList<ConversationSummaryDto>>.Success(convs);
    }
}
