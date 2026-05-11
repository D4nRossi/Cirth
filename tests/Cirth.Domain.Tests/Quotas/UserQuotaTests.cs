using Cirth.Domain.Quotas;
using Cirth.Shared;
using FluentAssertions;

namespace Cirth.Domain.Tests.Quotas;

public sealed class UserQuotaTests
{
    private static readonly UserId UserId = new(Guid.NewGuid());
    private static readonly TenantId TenantId = new(Guid.NewGuid());

    [Fact]
    public void CreateDefault_ShouldHaveDefaultLimits()
    {
        var quota = UserQuota.CreateDefault(UserId, TenantId);

        quota.DailyTokenLimit.Should().Be(100_000);
        quota.DailyUploadLimit.Should().Be(50);
        quota.StorageLimitBytes.Should().Be(5L * 1024 * 1024 * 1024);
        quota.TokensUsedToday.Should().Be(0);
        quota.UploadsToday.Should().Be(0);
    }

    [Fact]
    public void CanConsumeTokens_WhenUnderLimit_ShouldReturnTrue()
    {
        var quota = UserQuota.CreateDefault(UserId, TenantId);
        quota.CanConsumeTokens(50_000).Should().BeTrue();
    }

    [Fact]
    public void CanConsumeTokens_WhenOverLimit_ShouldReturnFalse()
    {
        var quota = UserQuota.CreateDefault(UserId, TenantId);
        quota.ConsumeTokens(90_000);
        quota.CanConsumeTokens(20_000).Should().BeFalse();
    }

    [Fact]
    public void RecordUpload_ShouldIncrementCountersAndStorage()
    {
        var quota = UserQuota.CreateDefault(UserId, TenantId);
        quota.RecordUpload(1024 * 1024);

        quota.UploadsToday.Should().Be(1);
        quota.StorageUsedBytes.Should().Be(1024 * 1024);
    }

    [Fact]
    public void CanUpload_WhenUploadLimitReached_ShouldReturnFalse()
    {
        var quota = UserQuota.CreateDefault(UserId, TenantId);
        for (int i = 0; i < 50; i++)
            quota.RecordUpload(1024);

        quota.CanUpload(1024).Should().BeFalse();
    }

    [Fact]
    public void ReleaseStorage_ShouldDecreaseStorageUsed()
    {
        var quota = UserQuota.CreateDefault(UserId, TenantId);
        quota.RecordUpload(2048);
        quota.ReleaseStorage(1024);

        quota.StorageUsedBytes.Should().Be(1024);
    }
}
