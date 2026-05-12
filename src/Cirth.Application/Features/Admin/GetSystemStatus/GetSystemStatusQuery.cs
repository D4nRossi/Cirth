using Cirth.Application.Common.Ports;
using Cirth.Shared;
using MediatR;

namespace Cirth.Application.Features.Admin.GetSystemStatus;

public sealed record GetSystemStatusQuery : IRequest<Result<SystemStatusDto>>;

public sealed record SystemStatusDto(
    IReadOnlyList<ServiceHealthStatus> Services,
    DateTimeOffset CheckedAt);

internal sealed class GetSystemStatusQueryHandler(
    ISystemHealthService healthService)
    : IRequestHandler<GetSystemStatusQuery, Result<SystemStatusDto>>
{
    public async Task<Result<SystemStatusDto>> Handle(GetSystemStatusQuery _, CancellationToken ct)
    {
        var services = await healthService.CheckAllAsync(ct);
        return Result<SystemStatusDto>.Success(new(services, DateTimeOffset.UtcNow));
    }
}
