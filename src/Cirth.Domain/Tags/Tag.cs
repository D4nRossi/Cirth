using Cirth.Domain.Common;
using Cirth.Shared;

namespace Cirth.Domain.Tags;

public sealed class Tag : Entity<TagId>, IAggregateRoot
{
    public TenantId TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Color { get; private set; }

    private Tag() { }

    public static Tag Create(TenantId tenantId, string name, string? color = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name is required.", nameof(name));

        return new Tag
        {
            Id = TagId.New(),
            TenantId = tenantId,
            Name = name.Trim().ToLowerInvariant(),
            Color = color?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Tag name is required.", nameof(newName));
        Name = newName.Trim().ToLowerInvariant();
        Touch();
    }

    public void SetColor(string? color)
    {
        Color = color?.Trim();
        Touch();
    }
}
