using Cirth.Domain.Common;
using Cirth.Shared;

namespace Cirth.Domain.Users;

public sealed class User : Entity<UserId>, IAggregateRoot
{
    public TenantId TenantId { get; private set; }
    public Guid EntraObjectId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }

    private User() { } // EF

    public static User Provision(
        TenantId tenantId,
        Guid entraObjectId,
        string email,
        string displayName,
        UserRole role)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        return new User
        {
            Id = UserId.New(),
            TenantId = tenantId,
            EntraObjectId = entraObjectId,
            Email = email.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Promote()
    {
        Role = UserRole.Admin;
        Touch();
    }

    public void Demote()
    {
        Role = UserRole.User;
        Touch();
    }
}

public enum UserRole
{
    User = 0,
    Admin = 1
}
