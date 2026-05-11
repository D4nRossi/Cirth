using Cirth.Domain.Users;
using Cirth.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cirth.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(u => u.Id);
        b.Property(u => u.Id).HasConversion(id => id.Value, v => new UserId(v));
        b.Property(u => u.TenantId).HasConversion(id => id.Value, v => new TenantId(v)).IsRequired();
        b.Property(u => u.EntraObjectId).IsRequired();
        b.Property(u => u.Email).HasMaxLength(320).IsRequired();
        b.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();
        b.Property(u => u.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(u => u.CreatedAt).IsRequired();
        b.Property(u => u.UpdatedAt).IsRequired();
        b.HasIndex(u => u.EntraObjectId).IsUnique();
        b.HasIndex(u => u.TenantId);
    }
}
