using Cirth.Domain.Common;
using Cirth.Shared;

namespace Cirth.Domain.Documents;

public sealed class Document : Entity<DocumentId>, IAggregateRoot
{
    public TenantId TenantId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public DocumentSourceType SourceType { get; private set; }
    public string? Author { get; private set; }
    public DocumentStatus Status { get; private set; }
    public DocumentVersionId? CurrentVersionId { get; private set; }

    private readonly List<DocumentVersion> _versions = [];
    public IReadOnlyList<DocumentVersion> Versions => _versions.AsReadOnly();

    private Document() { } // EF

    public static Document Create(
        TenantId tenantId,
        string title,
        DocumentSourceType sourceType,
        string? author = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        return new Document
        {
            Id = DocumentId.New(),
            TenantId = tenantId,
            Title = title.Trim(),
            SourceType = sourceType,
            Author = author?.Trim(),
            Status = DocumentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public DocumentVersion AddVersion(
        string contentHash,
        string storageKey,
        long sizeBytes,
        string mimeType)
    {
        // Marca a versão anterior como não-atual.
        foreach (var existing in _versions)
            existing.MarkAsHistorical();

        var newVersion = DocumentVersion.Create(
            documentId: Id,
            versionNumber: _versions.Count + 1,
            contentHash: contentHash,
            storageKey: storageKey,
            sizeBytes: sizeBytes,
            mimeType: mimeType);

        _versions.Add(newVersion);
        CurrentVersionId = newVersion.Id;
        Status = DocumentStatus.Pending;
        Touch();

        return newVersion;
    }

    public void MarkAsIndexed()
    {
        Status = DocumentStatus.Indexed;
        Touch();
    }

    public void MarkAsFailed(string reason)
    {
        Status = DocumentStatus.Failed;
        Touch();
    }
}

public enum DocumentStatus
{
    Pending = 0,
    Processing = 1,
    Indexed = 2,
    Failed = 3
}

public enum DocumentSourceType
{
    Pdf = 0,
    Docx = 1,
    Markdown = 2,
    Text = 3,
    Html = 4,
    WebLink = 5
}
