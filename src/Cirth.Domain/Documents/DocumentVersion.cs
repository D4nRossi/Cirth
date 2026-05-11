using Cirth.Domain.Common;
using Cirth.Shared;

namespace Cirth.Domain.Documents;

public sealed class DocumentVersion : Entity<DocumentVersionId>
{
    public DocumentId DocumentId { get; private set; }
    public int VersionNumber { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string MimeType { get; private set; } = string.Empty;
    public bool IsCurrent { get; private set; }

    private DocumentVersion() { } // EF

    internal static DocumentVersion Create(
        DocumentId documentId,
        int versionNumber,
        string contentHash,
        string storageKey,
        long sizeBytes,
        string mimeType) => new()
    {
        Id = DocumentVersionId.New(),
        DocumentId = documentId,
        VersionNumber = versionNumber,
        ContentHash = contentHash,
        StorageKey = storageKey,
        SizeBytes = sizeBytes,
        MimeType = mimeType,
        IsCurrent = true,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    internal void MarkAsHistorical()
    {
        IsCurrent = false;
        Touch();
    }
}
