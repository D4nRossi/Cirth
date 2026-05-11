using Cirth.Domain.Common;
using Cirth.Shared;

namespace Cirth.Domain.Conversations;

public sealed class Message : Entity<MessageId>
{
    public ConversationId ConversationId { get; private set; }
    public MessageRole Role { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public Guid[] CitedChunkIds { get; private set; } = [];
    public int? TokensUsed { get; private set; }
    public string? Model { get; private set; }

    private Message() { }

    internal static Message Create(
        ConversationId conversationId,
        MessageRole role,
        string content,
        IEnumerable<Guid> citedChunkIds,
        int? tokensUsed,
        string? model)
    {
        return new Message
        {
            Id = MessageId.New(),
            ConversationId = conversationId,
            Role = role,
            Content = content,
            CitedChunkIds = citedChunkIds.ToArray(),
            TokensUsed = tokensUsed,
            Model = model,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }
}

public enum MessageRole
{
    User = 0,
    Assistant = 1
}
