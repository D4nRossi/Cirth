using Cirth.Domain.Quotas;
using Cirth.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cirth.Infrastructure.Persistence.Configurations;

internal sealed class UserQuotaConfiguration : IEntityTypeConfiguration<UserQuota>
{
    public void Configure(EntityTypeBuilder<UserQuota> b)
    {
        b.ToTable("user_quotas");
        b.HasKey(q => q.Id);
        b.Property(q => q.Id).HasConversion(id => id.Value, v => new UserId(v));
        b.Property(q => q.TenantId).HasConversion(id => id.Value, v => new TenantId(v)).IsRequired();
        b.Property(q => q.DailyTokenLimit).IsRequired();
        b.Property(q => q.DailyUploadLimit).IsRequired();
        b.Property(q => q.StorageLimitBytes).IsRequired();
        b.Property(q => q.TokensUsedToday).IsRequired();
        b.Property(q => q.UploadsToday).IsRequired();
        b.Property(q => q.StorageUsedBytes).IsRequired();
        b.Property(q => q.ResetAt).IsRequired();
        b.Property(q => q.CreatedAt).IsRequired();
        b.Property(q => q.UpdatedAt).IsRequired();
    }
}
