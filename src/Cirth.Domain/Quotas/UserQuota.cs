using Cirth.Domain.Common;
using Cirth.Shared;

namespace Cirth.Domain.Quotas;

public sealed class UserQuota : Entity<UserId>
{
    public TenantId TenantId { get; private set; }
    public int DailyTokenLimit { get; private set; }
    public int DailyUploadLimit { get; private set; }
    public long StorageLimitBytes { get; private set; }
    public int TokensUsedToday { get; private set; }
    public int UploadsToday { get; private set; }
    public long StorageUsedBytes { get; private set; }
    public DateTimeOffset ResetAt { get; private set; }

    private UserQuota() { }

    public static UserQuota CreateDefault(UserId userId, TenantId tenantId)
    {
        return new UserQuota
        {
            Id = userId,
            TenantId = tenantId,
            DailyTokenLimit = 100_000,
            DailyUploadLimit = 50,
            StorageLimitBytes = 5L * 1024 * 1024 * 1024, // 5 GB
            TokensUsedToday = 0,
            UploadsToday = 0,
            StorageUsedBytes = 0,
            ResetAt = NextMidnightUtc(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public bool CanConsumeTokens(int tokens)
    {
        EnsureReset();
        return TokensUsedToday + tokens <= DailyTokenLimit;
    }

    public bool CanUpload(long fileSizeBytes)
    {
        EnsureReset();
        return UploadsToday < DailyUploadLimit
            && StorageUsedBytes + fileSizeBytes <= StorageLimitBytes;
    }

    public void ConsumeTokens(int tokens)
    {
        EnsureReset();
        TokensUsedToday += tokens;
        Touch();
    }

    public void RecordUpload(long fileSizeBytes)
    {
        EnsureReset();
        UploadsToday++;
        StorageUsedBytes += fileSizeBytes;
        Touch();
    }

    public void ReleaseStorage(long fileSizeBytes)
    {
        StorageUsedBytes = Math.Max(0, StorageUsedBytes - fileSizeBytes);
        Touch();
    }

    public void UpdateLimits(int? dailyTokenLimit, int? dailyUploadLimit, long? storageLimitBytes)
    {
        if (dailyTokenLimit.HasValue) DailyTokenLimit = dailyTokenLimit.Value;
        if (dailyUploadLimit.HasValue) DailyUploadLimit = dailyUploadLimit.Value;
        if (storageLimitBytes.HasValue) StorageLimitBytes = storageLimitBytes.Value;
        Touch();
    }

    private void EnsureReset()
    {
        if (DateTimeOffset.UtcNow >= ResetAt)
        {
            TokensUsedToday = 0;
            UploadsToday = 0;
            ResetAt = NextMidnightUtc();
            Touch();
        }
    }

    private static DateTimeOffset NextMidnightUtc()
        => DateTimeOffset.UtcNow.Date.AddDays(1);
}
