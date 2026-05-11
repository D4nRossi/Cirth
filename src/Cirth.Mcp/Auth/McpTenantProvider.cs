using Cirth.Application.Common.Ports;
using Cirth.Shared;
using System.Security.Claims;

namespace Cirth.Mcp.Auth;

internal sealed class McpTenantProvider(IHttpContextAccessor httpContextAccessor) : ITenantProvider
{
    public TenantId CurrentTenantId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirstValue("cirth:tenant_id");
            return claim is not null && Guid.TryParse(claim, out var id)
                ? new TenantId(id)
                : throw new UnauthorizedAccessException("Valid API Key required.");
        }
    }

    public UserId CurrentUserId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirstValue("cirth:user_id");
            return claim is not null && Guid.TryParse(claim, out var id)
                ? new UserId(id)
                : throw new UnauthorizedAccessException("Valid API Key required.");
        }
    }

    public bool IsAdmin
        => httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
}
