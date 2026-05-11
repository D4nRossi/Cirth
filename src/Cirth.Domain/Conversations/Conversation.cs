using Cirth.Domain.Common;
using Cirth.Shared;

namespace Cirth.Domain.Conversations;

public sealed class Conversation : Entity<ConversationId>, IAggregateRoot
{
    public TenantId TenantId { get; private set; }
    public UserId UserId { get; private set; }
    public string? Title { get; private set; }

    private readonly List<Message> _messages = [];
    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();

    private Conversation() { }

    public static Conversation Create(TenantId tenantId, UserId userId, string? title = null)
    {
        return new Conversation
        {
            Id = ConversationId.New(),
            TenantId = tenantId,
            UserId = userId,
            Title = title?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public Message AddUserMessage(string content)
    {
        var msg = Message.Create(Id, MessageRole.User, content, [], null, null);
        _messages.Add(msg);
        Touch();
        return msg;
    }

    public Message AddAssistantMessage(string content, IEnumerable<Guid> citedChunkIds, int? tokensUsed, string? model)
    {
        var msg = Message.Create(Id, MessageRole.Assistant, content, citedChunkIds, tokensUsed, model);
        _messages.Add(msg);
        Touch();
        return msg;
    }

    public void SetTitle(string title)
    {
        Title = title.Trim();
        Touch();
    }
}
