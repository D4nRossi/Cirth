using Cirth.Domain.Documents;
using Cirth.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cirth.Infrastructure.Persistence.Configurations;

internal sealed class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> b)
    {
        b.ToTable("document_versions");
        b.HasKey(v => v.Id);
        b.Property(v => v.Id).HasConversion(id => id.Value, v => new DocumentVersionId(v));
        b.Property(v => v.DocumentId).HasConversion(id => id.Value, v => new DocumentId(v)).IsRequired();
        b.Property(v => v.VersionNumber).IsRequired();
        b.Property(v => v.ContentHash).HasMaxLength(64).IsRequired();
        b.Property(v => v.StorageKey).HasMaxLength(500).IsRequired();
        b.Property(v => v.SizeBytes).IsRequired();
        b.Property(v => v.MimeType).HasMaxLength(100).IsRequired();
        b.Property(v => v.IsCurrent).IsRequired();
        b.Property(v => v.CreatedAt).IsRequired();
        b.Property(v => v.UpdatedAt).IsRequired();
        b.HasIndex(v => v.DocumentId);
        b.HasIndex(v => new { v.DocumentId, v.IsCurrent });
    }
}
