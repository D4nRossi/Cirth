using Cirth.Domain.Documents;
using Cirth.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cirth.Infrastructure.Persistence.Configurations;

internal sealed class ChunkConfiguration : IEntityTypeConfiguration<Chunk>
{
    public void Configure(EntityTypeBuilder<Chunk> b)
    {
        b.ToTable("chunks");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).HasConversion(id => id.Value, v => new ChunkId(v));
        b.Property(c => c.TenantId).HasConversion(id => id.Value, v => new TenantId(v)).IsRequired();
        b.Property(c => c.DocumentVersionId).HasConversion(id => id.Value, v => new DocumentVersionId(v)).IsRequired();
        b.Property(c => c.Ordinal).IsRequired();
        b.Property(c => c.Content).IsRequired();
        b.Property(c => c.TokenCount).IsRequired();
        b.Property(c => c.QdrantPointId).IsRequired();
        b.Property(c => c.IsCurrent).IsRequired();
        b.Property(c => c.CreatedAt).IsRequired();
        b.Property(c => c.UpdatedAt).IsRequired();

        // GIN index for full-text search — created via SQL migration
        b.HasIndex(c => new { c.TenantId, c.IsCurrent });
    }
}
