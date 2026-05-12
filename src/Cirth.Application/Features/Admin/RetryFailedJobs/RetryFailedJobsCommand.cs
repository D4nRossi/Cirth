using Cirth.Application.Common;
using Cirth.Application.Common.Ports;
using Cirth.Shared;
using MediatR;

namespace Cirth.Application.Features.Admin.RetryFailedJobs;

/// <summary>
/// Resets every job in Failed status back to Pending. Admin-only — typically called after
/// infra (e.g., AI deployment) was down and many jobs hit max attempts.
/// Bypasses tenant scoping: the recovery is a global operation.
/// </summary>
public sealed record RetryFailedJobsCommand : IRequest<Result<int>>, IBypassTenantScope;

internal sealed class RetryFailedJobsCommandHandler(
    IJobQueue jobQueue) : IRequestHandler<RetryFailedJobsCommand, Result<int>>
{
    public async Task<Result<int>> Handle(RetryFailedJobsCommand cmd, CancellationToken ct)
    {
        var count = await jobQueue.RetryFailedJobsAsync(ct);
        return Result<int>.Success(count);
    }
}
