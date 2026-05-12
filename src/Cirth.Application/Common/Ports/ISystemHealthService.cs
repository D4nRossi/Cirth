namespace Cirth.Application.Common.Ports;

public sealed record ServiceHealthStatus(
    string Name,
    bool IsHealthy,
    string? Detail,
    long LatencyMs);

public interface ISystemHealthService
{
    Task<IReadOnlyList<ServiceHealthStatus>> CheckAllAsync(CancellationToken ct);
}
