using Cirth.Domain.Common;
using Cirth.Shared;

namespace Cirth.Domain.Tenants;

public sealed class Tenant : Entity<TenantId>, IAggregateRoot
{
    public string Name { get; private set; } = string.Empty;

    private Tenant() { } // EF

    public static Tenant Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tenant name is required.", nameof(name));

        return new Tenant
        {
            Id = TenantId.New(),
            Name = name.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Tenant name is required.", nameof(newName));

        Name = newName.Trim();
        Touch();
    }
}
