using Cirth.Application.Common.Ports;
using Cirth.Shared;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Cirth.Infrastructure.Ai.Adapters;

internal sealed class QdrantVectorStore(QdrantClient qdrant) : IVectorStore
{
    private const string CollectionName = "cirth_chunks";
    private const string SavedAnswersCollection = "cirth_saved_answers";

    public async Task UpsertAsync(
        TenantId tenantId,
        ChunkId chunkId,
        ReadOnlyMemory<float> embedding,
        Dictionary<string, object> payload,
        CancellationToken ct)
    {
        var collection = payload.ContainsKey("type") && payload["type"]?.ToString() == "saved_answer"
            ? SavedAnswersCollection
            : CollectionName;

        await EnsureCollectionAsync(collection, embedding.Length, ct);

        payload["tenant_id"] = tenantId.Value.ToString();

        var point = new PointStruct
        {
            Id = new PointId { Uuid = chunkId.Value.ToString() },
            Vectors = embedding.ToArray()
        };
        foreach (var (key, value) in payload)
            point.Payload[key] = value?.ToString() ?? string.Empty;

        await qdrant.UpsertAsync(collection, [point], cancellationToken: ct);
    }

    public async Task<IReadOnlyList<VectorHit>> SearchAsync(
        TenantId tenantId,
        ReadOnlyMemory<float> queryEmbedding,
        int topK,
        CancellationToken ct)
    {
        await EnsureCollectionAsync(CollectionName, queryEmbedding.Length, ct);

        var filter = new Filter
        {
            Must = { new Condition { Field = new FieldCondition { Key = "tenant_id", Match = new Match { Text = tenantId.Value.ToString() } } } }
        };

        var results = await qdrant.SearchAsync(
            CollectionName,
            queryEmbedding.ToArray(),
            filter: filter,
            limit: (ulong)topK,
            cancellationToken: ct);

        return results
            .Select(r => new VectorHit(new ChunkId(Guid.Parse(r.Id.Uuid)), r.Score))
            .ToList();
    }

    public async Task DeleteAsync(TenantId tenantId, IEnumerable<ChunkId> chunkIds, CancellationToken ct)
    {
        // Qdrant.Client 1.12+ DeleteAsync accepts IReadOnlyList<Guid> for UUID-based point IDs
        var ids = chunkIds.Select(c => c.Value).ToList();
        if (ids.Count == 0) return;
        await qdrant.DeleteAsync(CollectionName, (IReadOnlyList<Guid>)ids, cancellationToken: ct);
    }

    private async Task EnsureCollectionAsync(string name, int dimension, CancellationToken ct)
    {
        var collections = await qdrant.ListCollectionsAsync(ct);
        if (collections.Any(c => c == name)) return;

        // CreateCollectionAsync takes VectorParams directly (not wrapped in VectorsConfig)
        await qdrant.CreateCollectionAsync(name,
            new VectorParams { Size = (ulong)dimension, Distance = Distance.Cosine },
            cancellationToken: ct);
    }
}
