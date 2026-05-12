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
        var version = await db.Database
            .SqlQueryRaw<string>("SELECT version()")
            .FirstAsync();
        return version.Split(',')[0].Replace("PostgreSQL ", "v");
    }

    private async Task<string?> CheckRedisAsync()
    {
        var redisDb = redis.GetDatabase();
        var latency = await redisDb.PingAsync();
        var info = await redis.GetServer(redis.GetEndPoints()[0]).InfoAsync("server");
        var serverGroup = info.FirstOrDefault(g => g.Key == "server");
        var version = serverGroup?.FirstOrDefault(kv => kv.Key == "redis_version").Value ?? "?";
        return $"v{version} ({latency.TotalMilliseconds:F0}ms round-trip)";
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
