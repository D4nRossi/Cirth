using Cirth.Shared;

namespace Cirth.Application.Common.Ports;

/// <summary>
/// Provedor do tenant atual, resolvido por contexto (HTTP claim, API key, ou stdio env).
/// </summary>
public interface ITenantProvider
{
    TenantId CurrentTenantId { get; }
    UserId CurrentUserId { get; }
    bool IsAdmin { get; }
}

/// <summary>
/// Storage de objetos S3-compatible (MinIO em V1).
/// </summary>
public interface IObjectStorage
{
    Task<string> PutAsync(string bucket, string key, Stream content, string contentType, CancellationToken ct);
    Task<Stream> GetAsync(string bucket, string key, CancellationToken ct);
    Task DeleteAsync(string bucket, string key, CancellationToken ct);
}

/// <summary>
/// Parser de documento por mime type.
/// </summary>
public interface IDocumentParser
{
    bool CanHandle(string mimeType);
    Task<string> ExtractTextAsync(Stream content, CancellationToken ct);
}

/// <summary>
/// Chunker textual respeitando limites de tokens e estrutura.
/// </summary>
public interface IChunker
{
    IReadOnlyList<TextChunk> Chunk(string text, int maxTokens, int overlapTokens);
}

public sealed record TextChunk(string Content, int TokenCount, int Ordinal);

/// <summary>
/// Vector store (Qdrant em V1). Operações tenant-scoped via filtro de payload.
/// </summary>
public interface IVectorStore
{
    Task UpsertAsync(TenantId tenantId, ChunkId chunkId, ReadOnlyMemory<float> embedding, Dictionary<string, object> payload, CancellationToken ct);
    Task<IReadOnlyList<VectorHit>> SearchAsync(TenantId tenantId, ReadOnlyMemory<float> queryEmbedding, int topK, CancellationToken ct);
    Task DeleteAsync(TenantId tenantId, IEnumerable<ChunkId> chunkIds, CancellationToken ct);
}

public sealed record VectorHit(ChunkId ChunkId, double Score);

/// <summary>
/// Fila de jobs em Postgres (worker poll).
/// </summary>
public interface IJobQueue
{
    Task EnqueueAsync(string type, object payload, CancellationToken ct);
    Task<IReadOnlyList<JobRecord>> DequeueAsync(int batchSize, CancellationToken ct);
    Task CompleteAsync(JobId jobId, CancellationToken ct);
    Task FailAsync(JobId jobId, string error, CancellationToken ct);
}

public sealed record JobRecord(JobId Id, string Type, string PayloadJson, int Attempts, int MaxAttempts);

/// <summary>
/// BM25 full-text search against the chunks table (Postgres tsvector).
/// Implemented in Infrastructure where raw SQL is available.
/// </summary>
public interface IBm25SearchService
{
    Task<IReadOnlyList<Guid>> SearchChunksAsync(Guid tenantId, string query, int topK, CancellationToken ct);
}
