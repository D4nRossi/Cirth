using Cirth.Application.Common.Ports;
using Cirth.Shared;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Cirth.Infrastructure.Auth;

/// <summary>
/// Resolves TenantId and UserId from the authenticated HTTP context.
/// Claims are populated after user provisioning via ProvisionUserCommand.
/// </summary>
public sealed class HttpTenantProvider(IHttpContextAccessor httpContextAccessor) : ITenantProvider
{
    public TenantId CurrentTenantId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirstValue("cirth:tenant_id");
            return claim is not null && Guid.TryParse(claim, out var id)
                ? new TenantId(id)
                : throw new InvalidOperationException("TenantId not found in claims. Is the user provisioned?");
        }
    }

    public UserId CurrentUserId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirstValue("cirth:user_id");
            return claim is not null && Guid.TryParse(claim, out var id)
                ? new UserId(id)
                : throw new InvalidOperationException("UserId not found in claims.");
        }
    }

    public bool IsAdmin
        => httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
}
