using Cirth.Domain.Documents;
using Cirth.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cirth.Infrastructure.Persistence.Configurations;

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> b)
    {
        b.ToTable("documents");
        b.HasKey(d => d.Id);
        b.Property(d => d.Id).HasConversion(id => id.Value, v => new DocumentId(v));
        b.Property(d => d.TenantId).HasConversion(id => id.Value, v => new TenantId(v)).IsRequired();
        b.Property(d => d.Title).HasMaxLength(500).IsRequired();
        b.Property(d => d.SourceType).HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(d => d.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(d => d.Author).HasMaxLength(200);
        b.Property(d => d.CurrentVersionId).HasConversion(
            id => id.HasValue ? id.Value.Value : (Guid?)null,
            v => v.HasValue ? new DocumentVersionId(v.Value) : null);
        b.Property(d => d.CreatedAt).IsRequired();
        b.Property(d => d.UpdatedAt).IsRequired();

        b.HasMany(d => d.Versions)
         .WithOne()
         .HasForeignKey(v => v.DocumentId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(d => d.TenantId);
        b.HasIndex(d => new { d.TenantId, d.Status });
    }
}
