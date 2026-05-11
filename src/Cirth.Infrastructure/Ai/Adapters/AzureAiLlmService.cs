using Cirth.Application.Common.Ports;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace Cirth.Infrastructure.Ai.Adapters;

internal sealed class AzureAiLlmService(IChatClient chatClient) : ILlmChatService
{
    public async IAsyncEnumerable<string> StreamChatAsync(
        string systemPrompt,
        IEnumerable<ChatMessageDto> history,
        string userMessage,
        string? model = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var messages = BuildMessages(systemPrompt, history, userMessage);
        var options = model is not null ? new ChatOptions { ModelId = model } : null;

        await foreach (var update in chatClient.GetStreamingResponseAsync(messages, options, ct))
        {
            if (!string.IsNullOrEmpty(update.Text))
                yield return update.Text;
        }
    }

    public async Task<string> CompleteChatAsync(
        string systemPrompt,
        string userMessage,
        string? model = null,
        CancellationToken ct = default)
    {
        var messages = BuildMessages(systemPrompt, [], userMessage);
        var options = model is not null ? new ChatOptions { ModelId = model } : null;
        var response = await chatClient.GetResponseAsync(messages, options, ct);
        return response.Text ?? string.Empty;
    }

    public Task<int> EstimateTokensAsync(string text, CancellationToken ct = default)
    {
        // Rough estimation: 1 token ≈ 4 characters
        return Task.FromResult(text.Length / 4);
    }

    private static IList<ChatMessage> BuildMessages(
        string systemPrompt,
        IEnumerable<ChatMessageDto> history,
        string userMessage)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt)
        };
        foreach (var h in history)
        {
            var role = h.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? ChatRole.User : ChatRole.Assistant;
            messages.Add(new(role, h.Content));
        }
        messages.Add(new(ChatRole.User, userMessage));
        return messages;
    }
}
