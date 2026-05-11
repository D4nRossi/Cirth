namespace Cirth.Application.Common.Ports;

public interface ILlmChatService
{
    IAsyncEnumerable<string> StreamChatAsync(
        string systemPrompt,
        IEnumerable<ChatMessageDto> history,
        string userMessage,
        string? model = null,
        CancellationToken ct = default);

    Task<string> CompleteChatAsync(
        string systemPrompt,
        string userMessage,
        string? model = null,
        CancellationToken ct = default);

    Task<int> EstimateTokensAsync(string text, CancellationToken ct = default);
}

public interface IEmbeddingService
{
    Task<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken ct = default);
    Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default);
}

public record ChatMessageDto(string Role, string Content);
