using Cirth.Shared;

namespace Cirth.Application.Common.Ports;

public interface IDbContextAccessor
{
    void SetTenant(TenantId tenantId);
}
