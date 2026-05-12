using Cirth.Application.Common.Ports;
using Cirth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Minio;
using Qdrant.Client;
using StackExchange.Redis;
using System.Diagnostics;

namespace Cirth.Infrastructure.Health;

internal sealed class SystemHealthService(
    AppDbContext db,
    IConnectionMultiplexer redis,
    QdrantClient qdrant,
    IMinioClient minio,
    IChatClient chatClient,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IConfiguration config) : ISystemHealthService
{
    public async Task<IReadOnlyList<ServiceHealthStatus>> CheckAllAsync(CancellationToken ct)
    {
        var tasks = new[]
        {
            CheckAsync("PostgreSQL",          CheckPostgresAsync,  ct),
            CheckAsync("Redis",               CheckRedisAsync,     ct),
            CheckAsync("Qdrant",              CheckQdrantAsync,    ct),
            CheckAsync("MinIO",               CheckMinioAsync,     ct),
            CheckAsync("Azure AI — Chat",     CheckChatAsync,      ct, timeoutSeconds: 15),
            CheckAsync("Azure AI — Embeddings", CheckEmbeddingAsync, ct, timeoutSeconds: 15),
        };

        return await Task.WhenAll(tasks);
    }

    private static async Task<ServiceHealthStatus> CheckAsync(
        string name, Func<CancellationToken, Task<string?>> check, CancellationToken ct, int timeoutSeconds = 5)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            var detail = await check(timeout.Token);
            return new ServiceHealthStatus(name, true, detail, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            // Strip multi-line stacktrace info — keep first line for the card UI.
            var msg = ex.Message.Split('\n')[0].Trim();
            return new ServiceHealthStatus(name, false, msg, sw.ElapsedMilliseconds);
        }
    }

    private async Task<string?> CheckPostgresAsync(CancellationToken ct)
    {
        var ok = await db.Database.CanConnectAsync(ct);
        if (!ok) throw new InvalidOperationException("Cannot connect to Postgres.");
        // EF Core's SqlQueryRaw<string> wraps the query and expects a column named "Value".
        // PostgreSQL is case-sensitive with double-quoted identifiers, so we alias explicitly.
        var version = await db.Database
            .SqlQueryRaw<string>("SELECT version() AS \"Value\"")
            .FirstAsync(ct);
        return version.Split(',')[0].Replace("PostgreSQL ", "v");
    }

    private async Task<string?> CheckRedisAsync(CancellationToken _)
    {
        // INFO is an admin command in StackExchange.Redis; using it requires
        // allowAdmin=true in the connection string, which broadens permissions
        // beyond what app code needs. PING alone confirms the server responds.
        var redisDb = redis.GetDatabase();
        var latency = await redisDb.PingAsync();
        return $"ping {latency.TotalMilliseconds:F0}ms";
    }

    private async Task<string?> CheckQdrantAsync(CancellationToken _)
    {
        var collections = await qdrant.ListCollectionsAsync();
        return $"{collections.Count} coleção(ões)";
    }

    private async Task<string?> CheckMinioAsync(CancellationToken _)
    {
        var result = await minio.ListBucketsAsync();
        return $"{result.Buckets.Count} bucket(s)";
    }

    /// <summary>
    /// Real chat liveness probe: sends a 1-token completion. No persistence — the response
    /// isn't stored, doesn't go through quotas, and isn't bound to any user/conversation.
    /// </summary>
    private async Task<string?> CheckChatAsync(CancellationToken ct)
    {
        var deployment = config["AzureAi:Chat:Deployment"] ?? "gpt-4.1";
        var messages = new List<ChatMessage> { new(ChatRole.User, "ok") };
        var options = new ChatOptions { MaxOutputTokens = 1 };
        var resp = await chatClient.GetResponseAsync(messages, options, ct);
        // The probe succeeded if no exception was thrown. Show the deployment id.
        _ = resp; // avoid unused warning
        return deployment;
    }

    /// <summary>
    /// Real embedding liveness probe: embeds a single short word. No persistence.
    /// </summary>
    private async Task<string?> CheckEmbeddingAsync(CancellationToken ct)
    {
        var deployment = config["AzureAi:Embedding:Deployment"] ?? "text-embedding-ada-002";
        var result = await embeddingGenerator.GenerateAsync(["ok"], cancellationToken: ct);
        return $"{deployment} ({result[0].Vector.Length}d)";
    }
}
