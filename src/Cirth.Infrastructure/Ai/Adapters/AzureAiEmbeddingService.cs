using Cirth.Application.Common.Ports;
using Microsoft.Extensions.AI;

namespace Cirth.Infrastructure.Ai.Adapters;

internal sealed class AzureAiEmbeddingService(
    IEmbeddingGenerator<string, Embedding<float>> generator) : IEmbeddingService
{
    public async Task<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken ct = default)
    {
        var result = await generator.GenerateAsync([text], cancellationToken: ct);
        return result[0].Vector;
    }

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> EmbedBatchAsync(
        IEnumerable<string> texts, CancellationToken ct = default)
    {
        var textList = texts.ToList();
        var results = await generator.GenerateAsync(textList, cancellationToken: ct);
        return results.Select(r => r.Vector).ToList();
    }
}
