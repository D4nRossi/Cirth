using Cirth.Application.Common.Ports;
using Cirth.Shared;

namespace Cirth.Worker;

/// <summary>
/// Scoped tenant context for the Worker. Each job scope sets the tenant before dispatching MediatR commands.
/// </summary>
public sealed class WorkerTenantProvider : ITenantProvider
{
    private TenantId? _tenantId;
    private UserId? _userId;

    public void Set(TenantId tenantId, UserId userId)
    {
        _tenantId = tenantId;
        _userId = userId;
    }

    public TenantId CurrentTenantId => _tenantId
        ?? throw new InvalidOperationException("Worker tenant not set. Call Set() before dispatching commands.");

    public UserId CurrentUserId => _userId
        ?? throw new InvalidOperationException("Worker user not set. Call Set() before dispatching commands.");

    public bool IsAdmin => false;
}
