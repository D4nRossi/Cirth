using Cirth.Domain.SavedAnswers;
using Cirth.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cirth.Infrastructure.Persistence.Configurations;

internal sealed class SavedAnswerConfiguration : IEntityTypeConfiguration<SavedAnswer>
{
    public void Configure(EntityTypeBuilder<SavedAnswer> b)
    {
        b.ToTable("saved_answers");
        b.HasKey(s => s.Id);
        b.Property(s => s.Id).HasConversion(id => id.Value, v => new SavedAnswerId(v));
        b.Property(s => s.TenantId).HasConversion(id => id.Value, v => new TenantId(v)).IsRequired();
        b.Property(s => s.Question).IsRequired();
        b.Property(s => s.Answer).IsRequired();
        b.Property(s => s.CitedChunkIds).HasColumnType("uuid[]");
        b.Property(s => s.TagIds).HasColumnType("uuid[]");
        b.Property(s => s.UsageCount).IsRequired();
        b.Property(s => s.UtilityScore).IsRequired();
        b.Property(s => s.QdrantPointId).IsRequired();
        b.Property(s => s.CreatedAt).IsRequired();
        b.Property(s => s.UpdatedAt).IsRequired();
        b.HasIndex(s => s.TenantId);
    }
}
