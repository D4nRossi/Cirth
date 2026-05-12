using Cirth.Application.Common.Ports;
using Cirth.Domain.Jobs;
using Cirth.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Cirth.Infrastructure.Persistence;

namespace Cirth.Infrastructure.Queue;

internal sealed class PostgresJobQueue(AppDbContext db, ILogger<PostgresJobQueue> logger) : IJobQueue
{
    public async Task EnqueueAsync(string type, object payload, CancellationToken ct)
    {
        // TenantId comes from db context
        var tenantId = db.CurrentTenantId ?? throw new InvalidOperationException("TenantId not set on DbContext.");
        var payloadJson = JsonSerializer.Serialize(payload);
        var job = IngestionJob.Create(tenantId, type, payloadJson);
        await db.Jobs.AddAsync(job, ct);
    }

    public async Task<IReadOnlyList<JobRecord>> DequeueAsync(int batchSize, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // Lock rows via PostgreSQL SKIP LOCKED to prevent double-processing
        var jobs = await db.Jobs
            .FromSqlInterpolated($"""
                SELECT * FROM jobs
                WHERE status IN ('Pending', 'Retrying')
                  AND next_run_at <= {now}
                ORDER BY next_run_at ASC
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

        foreach (var job in jobs)
            job.MarkProcessing();

        await db.SaveChangesAsync(ct);

        return jobs.Select(j => new JobRecord(j.Id, j.Type, j.PayloadJson, j.Attempts, j.MaxAttempts)).ToList();
    }

    public async Task CompleteAsync(JobId jobId, CancellationToken ct)
    {
        var job = await db.Jobs.FindAsync([jobId], ct);
        if (job is null) return;
        job.Complete();
        await db.SaveChangesAsync(ct);
    }

    public async Task FailAsync(JobId jobId, string error, CancellationToken ct)
    {
        var job = await db.Jobs.FindAsync([jobId], ct);
        if (job is null) return;
        job.Fail(error);
        logger.LogWarning("Job {JobId} failed (attempt {Attempt}/{Max}): {Error}",
            jobId.Value, job.Attempts, job.MaxAttempts, error);
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> RecoverStuckJobsAsync(TimeSpan threshold, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow - threshold;
        // Raw SQL — bypasses EF query filter (tenant-scoped) on purpose, since the
        // background recovery service runs at the global level (no tenant context).
        // Decision: if a job has hit MaxAttempts already by the time we recover it,
        // mark it permanently Failed; otherwise put it back on the queue.
        var affected = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE jobs
            SET status = CASE WHEN attempts >= max_attempts THEN 'Failed' ELSE 'Retrying' END,
                next_run_at = {DateTimeOffset.UtcNow},
                error = COALESCE(error, '') || ' [recovered from stuck Processing]',
                updated_at = {DateTimeOffset.UtcNow}
            WHERE status = 'Processing'
              AND updated_at < {cutoff}
            """, ct);

        if (affected > 0)
            logger.LogWarning("Recovered {Count} job(s) stuck in Processing (>{Threshold}m)",
                affected, threshold.TotalMinutes);

        return affected;
    }

    public async Task<int> RetryFailedJobsAsync(CancellationToken ct)
    {
        var affected = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE jobs
            SET status = 'Pending',
                attempts = 0,
                next_run_at = {DateTimeOffset.UtcNow},
                error = NULL,
                updated_at = {DateTimeOffset.UtcNow}
            WHERE status = 'Failed'
            """, ct);

        if (affected > 0)
            logger.LogInformation("Manually re-queued {Count} failed job(s)", affected);

        return affected;
    }
}
