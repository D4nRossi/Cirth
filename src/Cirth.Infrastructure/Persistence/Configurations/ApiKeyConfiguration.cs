using Cirth.Domain.Auth;
using Cirth.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cirth.Infrastructure.Persistence.Configurations;

internal sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> b)
    {
        b.ToTable("api_keys");
        b.HasKey(a => a.Id);
        b.Property(a => a.Id).HasConversion(id => id.Value, v => new ApiKeyId(v));
        b.Property(a => a.TenantId).HasConversion(id => id.Value, v => new TenantId(v)).IsRequired();
        b.Property(a => a.UserId).HasConversion(id => id.Value, v => new UserId(v)).IsRequired();
        b.Property(a => a.Name).HasMaxLength(100).IsRequired();
        b.Property(a => a.KeyHash).HasMaxLength(128).IsRequired();
        b.Property(a => a.LastUsedAt);
        b.Property(a => a.ExpiresAt);
        b.Property(a => a.Revoked).IsRequired();
        b.Property(a => a.CreatedAt).IsRequired();
        b.Property(a => a.UpdatedAt).IsRequired();
        b.HasIndex(a => a.KeyHash).IsUnique();
        b.HasIndex(a => new { a.TenantId, a.UserId });
    }
}

internal sealed class UserInviteConfiguration : IEntityTypeConfiguration<UserInvite>
{
    public void Configure(EntityTypeBuilder<UserInvite> b)
    {
        b.ToTable("user_invites");
        b.HasKey(i => i.Id);
        b.Property(i => i.Id).HasConversion(id => id.Value, v => new UserInviteId(v));
        b.Property(i => i.TenantId).HasConversion(id => id.Value, v => new TenantId(v)).IsRequired();
        b.Property(i => i.InvitedBy).HasConversion(id => id.Value, v => new UserId(v)).IsRequired();
        b.Property(i => i.Email).HasMaxLength(320).IsRequired();
        b.Property(i => i.Token).HasMaxLength(64).IsRequired();
        b.Property(i => i.ExpiresAt).IsRequired();
        b.Property(i => i.Accepted).IsRequired();
        b.Property(i => i.CreatedAt).IsRequired();
        b.Property(i => i.UpdatedAt).IsRequired();
        b.HasIndex(i => i.Token).IsUnique();
        b.HasIndex(i => i.TenantId);
    }
}
