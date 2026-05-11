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
}
