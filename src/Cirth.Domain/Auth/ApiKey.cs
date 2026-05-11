using Cirth.Domain.Common;
using Cirth.Shared;

namespace Cirth.Domain.Auth;

public sealed class ApiKey : Entity<ApiKeyId>, IAggregateRoot
{
    public TenantId TenantId { get; private set; }
    public UserId UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;
    public DateTimeOffset? LastUsedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public bool Revoked { get; private set; }

    private ApiKey() { }

    public static ApiKey Create(
        TenantId tenantId,
        UserId userId,
        string name,
        string keyHash,
        DateTimeOffset? expiresAt = null)
    {
        return new ApiKey
        {
            Id = ApiKeyId.New(),
            TenantId = tenantId,
            UserId = userId,
            Name = name.Trim(),
            KeyHash = keyHash,
            ExpiresAt = expiresAt,
            Revoked = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void RecordUsage()
    {
        LastUsedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Revoke()
    {
        Revoked = true;
        Touch();
    }

    public bool IsValid() => !Revoked && (ExpiresAt is null || ExpiresAt > DateTimeOffset.UtcNow);
}
