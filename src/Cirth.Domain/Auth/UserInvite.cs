using Cirth.Domain.Common;
using Cirth.Shared;

namespace Cirth.Domain.Auth;

public sealed class UserInvite : Entity<UserInviteId>, IAggregateRoot
{
    public TenantId TenantId { get; private set; }
    public UserId InvitedBy { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Token { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool Accepted { get; private set; }

    private UserInvite() { }

    public static UserInvite Create(TenantId tenantId, UserId invitedBy, string email)
    {
        return new UserInvite
        {
            Id = UserInviteId.New(),
            TenantId = tenantId,
            InvitedBy = invitedBy,
            Email = email.Trim().ToLowerInvariant(),
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            Accepted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public bool IsValid() => !Accepted && ExpiresAt > DateTimeOffset.UtcNow;

    public void Accept()
    {
        Accepted = true;
        Touch();
    }
}
