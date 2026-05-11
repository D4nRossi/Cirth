using Cirth.Domain.Common;
using Cirth.Shared;

namespace Cirth.Domain.Collections;

public sealed class Collection : Entity<CollectionId>, IAggregateRoot
{
    public TenantId TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private Collection() { }

    public static Collection Create(TenantId tenantId, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Collection name is required.", nameof(name));

        return new Collection
        {
            Id = CollectionId.New(),
            TenantId = tenantId,
            Name = name.Trim(),
            Description = description?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Collection name is required.", nameof(newName));
        Name = newName.Trim();
        Touch();
    }

    public void SetDescription(string? description)
    {
        Description = description?.Trim();
        Touch();
    }
}
