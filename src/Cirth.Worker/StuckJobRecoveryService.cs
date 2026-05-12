using Cirth.Application.Common.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cirth.Worker;

/// <summary>
/// Periodically sweeps the jobs table for entries stuck in Processing — typically caused by a
/// worker crash mid-job — and puts them back on the queue. Simple alternative to message
/// brokers + DLQ for V1.
/// </summary>
public sealed class StuckJobRecoveryService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<StuckJobRecoveryService> logger) : BackgroundService
{
    private readonly int _intervalSeconds = configuration.GetValue("Worker:StuckJobScanIntervalSeconds", 120);
    private readonly int _thresholdMinutes = configuration.GetValue("Worker:StuckJobThresholdMinutes", 10);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation(
            "StuckJobRecoveryService started. Scanning every {Interval}s, threshold={Threshold}m",
            _intervalSeconds, _thresholdMinutes);

        // First scan after one interval — give the worker a chance to start picking jobs up.
        await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var jobQueue = scope.ServiceProvider.GetRequiredService<IJobQueue>();
                await jobQueue.RecoverStuckJobsAsync(TimeSpan.FromMinutes(_thresholdMinutes), ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stuck job recovery scan failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(_intervalSeconds), ct);
        }
    }
}
