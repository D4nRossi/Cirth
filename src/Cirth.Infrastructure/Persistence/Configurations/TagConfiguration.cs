using Cirth.Domain.Tags;
using Cirth.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cirth.Infrastructure.Persistence.Configurations;

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> b)
    {
        b.ToTable("tags");
        b.HasKey(t => t.Id);
        b.Property(t => t.Id).HasConversion(id => id.Value, v => new TagId(v));
        b.Property(t => t.TenantId).HasConversion(id => id.Value, v => new TenantId(v)).IsRequired();
        b.Property(t => t.Name).HasMaxLength(100).IsRequired();
        b.Property(t => t.Color).HasMaxLength(7);
        b.Property(t => t.CreatedAt).IsRequired();
        b.Property(t => t.UpdatedAt).IsRequired();
        b.HasIndex(t => new { t.TenantId, t.Name }).IsUnique();
    }
}
