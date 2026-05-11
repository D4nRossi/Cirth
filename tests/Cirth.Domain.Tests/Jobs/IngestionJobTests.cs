using Cirth.Domain.Jobs;
using Cirth.Shared;
using FluentAssertions;

namespace Cirth.Domain.Tests.Jobs;

public sealed class IngestionJobTests
{
    private static readonly TenantId TenantId = new(Guid.NewGuid());

    [Fact]
    public void Create_ShouldReturnPendingJob()
    {
        var job = IngestionJob.Create(TenantId, "ProcessDocument", "{}", 3);

        job.Status.Should().Be(JobStatus.Pending);
        job.Attempts.Should().Be(0);
        job.MaxAttempts.Should().Be(3);
    }

    [Fact]
    public void MarkProcessing_ShouldIncrementAttempts()
    {
        var job = IngestionJob.Create(TenantId, "ProcessDocument", "{}");
        job.MarkProcessing();

        job.Status.Should().Be(JobStatus.Processing);
        job.Attempts.Should().Be(1);
    }

    [Fact]
    public void Complete_ShouldSetCompletedStatus()
    {
        var job = IngestionJob.Create(TenantId, "ProcessDocument", "{}");
        job.MarkProcessing();
        job.Complete();

        job.Status.Should().Be(JobStatus.Completed);
        job.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Fail_WhenBelowMaxAttempts_ShouldSetRetryingStatus()
    {
        var job = IngestionJob.Create(TenantId, "ProcessDocument", "{}", maxAttempts: 3);
        job.MarkProcessing();
        job.Fail("parse error");

        job.Status.Should().Be(JobStatus.Retrying);
        job.Error.Should().Be("parse error");
        job.NextRunAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Fail_WhenAtMaxAttempts_ShouldSetFailedStatus()
    {
        var job = IngestionJob.Create(TenantId, "ProcessDocument", "{}", maxAttempts: 1);
        job.MarkProcessing();
        job.Fail("fatal error");

        job.Status.Should().Be(JobStatus.Failed);
    }
}
