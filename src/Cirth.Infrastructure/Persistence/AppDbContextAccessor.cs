using Cirth.Application.Common.Ports;
using Cirth.Shared;

namespace Cirth.Infrastructure.Persistence;

internal sealed class AppDbContextAccessor(AppDbContext db) : IDbContextAccessor
{
    public void SetTenant(TenantId tenantId) => db.SetTenant(tenantId);
}
