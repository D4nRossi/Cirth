using Cirth.Application.Common.Ports;
using Cirth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using Qdrant.Client;
using StackExchange.Redis;
using System.Diagnostics;

namespace Cirth.Infrastructure.Health;

internal sealed class SystemHealthService(
    AppDbContext db,
    IConnectionMultiplexer redis,
    QdrantClient qdrant,
    IMinioClient minio,
    IConfiguration config) : ISystemHealthService
{
    public async Task<IReadOnlyList<ServiceHealthStatus>> CheckAllAsync(CancellationToken ct)
    {
        var tasks = new[]
        {
            CheckAsync("PostgreSQL", CheckPostgresAsync, ct),
            CheckAsync("Redis",      CheckRedisAsync,    ct),
            CheckAsync("Qdrant",     CheckQdrantAsync,   ct),
            CheckAsync("MinIO",      CheckMinioAsync,    ct),
            Task.FromResult(CheckAzureAi()),
        };

        return await Task.WhenAll(tasks);
    }

    private static async Task<ServiceHealthStatus> CheckAsync(
        string name, Func<Task<string?>> check, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            var detail = await check();
            return new ServiceHealthStatus(name, true, detail, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return new ServiceHealthStatus(name, false, ex.Message, sw.ElapsedMilliseconds);
        }
    }

    private async Task<string?> CheckPostgresAsync()
    {
        var ok = await db.Database.CanConnectAsync();
        if (!ok) throw new InvalidOperationException("Cannot connect to Postgres.");
        // EF Core's SqlQueryRaw<string> wraps the query and expects a column named "Value".
        // PostgreSQL is case-sensitive with double-quoted identifiers, so we alias explicitly.
        var version = await db.Database
            .SqlQueryRaw<string>("SELECT version() AS \"Value\"")
            .FirstAsync();
        return version.Split(',')[0].Replace("PostgreSQL ", "v");
    }

    private async Task<string?> CheckRedisAsync()
    {
        // INFO is an admin command in StackExchange.Redis; using it requires
        // allowAdmin=true in the connection string, which broadens permissions
        // beyond what app code needs. PING alone confirms the server responds.
        var redisDb = redis.GetDatabase();
        var latency = await redisDb.PingAsync();
        return $"ping {latency.TotalMilliseconds:F0}ms";
    }

    private async Task<string?> CheckQdrantAsync()
    {
        var collections = await qdrant.ListCollectionsAsync();
        return $"{collections.Count} coleção(ões)";
    }

    private async Task<string?> CheckMinioAsync()
    {
        var result = await minio.ListBucketsAsync();
        var buckets = result.Buckets;
        return $"{buckets.Count} bucket(s)";
    }

    private ServiceHealthStatus CheckAzureAi()
    {
        var endpoint = config["AzureAi:Endpoint"];
        var hasKey = !string.IsNullOrWhiteSpace(config["AzureAi:ApiKey"]);
        var chatDeploy = config["AzureAi:ChatDeployment"] ?? "gpt-4.1";
        var embedDeploy = config["AzureAi:EmbeddingDeployment"] ?? "text-embedding-3-small";

        if (string.IsNullOrWhiteSpace(endpoint) || !hasKey)
            return new ServiceHealthStatus("Azure AI Foundry", false, "Endpoint ou API key não configurados.", 0);

        return new ServiceHealthStatus("Azure AI Foundry", true,
            $"Chat: {chatDeploy} | Embed: {embedDeploy}", 0);
    }
}
