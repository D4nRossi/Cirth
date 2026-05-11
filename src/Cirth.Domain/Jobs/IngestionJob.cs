using Cirth.Domain.Common;
using Cirth.Shared;

namespace Cirth.Domain.Jobs;

public sealed class IngestionJob : Entity<JobId>, IAggregateRoot
{
    public TenantId TenantId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public JobStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public int MaxAttempts { get; private set; }
    public DateTimeOffset NextRunAt { get; private set; }
    public string? Error { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private IngestionJob() { }

    public static IngestionJob Create(TenantId tenantId, string type, string payloadJson, int maxAttempts = 3)
    {
        return new IngestionJob
        {
            Id = JobId.New(),
            TenantId = tenantId,
            Type = type,
            PayloadJson = payloadJson,
            Status = JobStatus.Pending,
            Attempts = 0,
            MaxAttempts = maxAttempts,
            NextRunAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkProcessing()
    {
        Status = JobStatus.Processing;
        Attempts++;
        Touch();
    }

    public void Complete()
    {
        Status = JobStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Fail(string error)
    {
        Error = error;
        if (Attempts >= MaxAttempts)
        {
            Status = JobStatus.Failed;
        }
        else
        {
            Status = JobStatus.Retrying;
            // Exponential backoff: 30s, 2min, 8min
            var delaySeconds = Math.Pow(4, Attempts) * 30;
            NextRunAt = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);
        }
        Touch();
    }
}

public enum JobStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Retrying = 4
}
