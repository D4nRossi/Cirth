using Cirth.Domain.Jobs;
using Cirth.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cirth.Infrastructure.Persistence.Configurations;

internal sealed class IngestionJobConfiguration : IEntityTypeConfiguration<IngestionJob>
{
    public void Configure(EntityTypeBuilder<IngestionJob> b)
    {
        b.ToTable("jobs");
        b.HasKey(j => j.Id);
        b.Property(j => j.Id).HasConversion(id => id.Value, v => new JobId(v));
        b.Property(j => j.TenantId).HasConversion(id => id.Value, v => new TenantId(v)).IsRequired();
        b.Property(j => j.Type).HasMaxLength(50).IsRequired();
        b.Property(j => j.PayloadJson).HasColumnType("jsonb").IsRequired();
        b.Property(j => j.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(j => j.Attempts).IsRequired();
        b.Property(j => j.MaxAttempts).IsRequired();
        b.Property(j => j.NextRunAt).IsRequired();
        b.Property(j => j.Error);
        b.Property(j => j.CompletedAt);
        b.Property(j => j.CreatedAt).IsRequired();
        b.Property(j => j.UpdatedAt).IsRequired();
        b.HasIndex(j => new { j.Status, j.NextRunAt });
    }
}
