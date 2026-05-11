using Cirth.Domain.Tenants;
using Cirth.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cirth.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("tenants");
        b.HasKey(t => t.Id);
        b.Property(t => t.Id).HasConversion(id => id.Value, v => new TenantId(v));
        b.Property(t => t.Name).HasMaxLength(200).IsRequired();
        b.Property(t => t.CreatedAt).IsRequired();
        b.Property(t => t.UpdatedAt).IsRequired();
    }
}
