using Cirth.Domain.Common;
using Cirth.Shared;

namespace Cirth.Domain.Documents;

public sealed class Chunk : Entity<ChunkId>
{
    public TenantId TenantId { get; private set; }
    public DocumentVersionId DocumentVersionId { get; private set; }
    public int Ordinal { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public int TokenCount { get; private set; }
    public Guid QdrantPointId { get; private set; }
    public bool IsCurrent { get; private set; }

    private Chunk() { }

    public static Chunk Create(
        TenantId tenantId,
        DocumentVersionId documentVersionId,
        int ordinal,
        string content,
        int tokenCount,
        Guid qdrantPointId)
    {
        return new Chunk
        {
            Id = ChunkId.New(),
            TenantId = tenantId,
            DocumentVersionId = documentVersionId,
            Ordinal = ordinal,
            Content = content,
            TokenCount = tokenCount,
            QdrantPointId = qdrantPointId,
            IsCurrent = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkAsHistorical()
    {
        IsCurrent = false;
        Touch();
    }
}
