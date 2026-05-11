using Cirth.Domain.Collections;
using Cirth.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cirth.Infrastructure.Persistence.Configurations;

internal sealed class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    public void Configure(EntityTypeBuilder<Collection> b)
    {
        b.ToTable("collections");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).HasConversion(id => id.Value, v => new CollectionId(v));
        b.Property(c => c.TenantId).HasConversion(id => id.Value, v => new TenantId(v)).IsRequired();
        b.Property(c => c.Name).HasMaxLength(200).IsRequired();
        b.Property(c => c.Description);
        b.Property(c => c.CreatedAt).IsRequired();
        b.Property(c => c.UpdatedAt).IsRequired();
        b.HasIndex(c => c.TenantId);
    }
}
