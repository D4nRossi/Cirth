using Cirth.Application.Features.Identity.ProvisionUser;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Cirth.Infrastructure.Auth;

/// <summary>
/// Intercepts authenticated requests and provisions the user on first login.
/// Adds cirth:tenant_id and cirth:user_id claims to the identity.
/// </summary>
public sealed class UserProvisioningMiddleware(RequestDelegate next)
{
    private static readonly string EntraTenantId = "c050c98c-b463-4591-ac3b-deb782c0ba6e";

    public async Task InvokeAsync(HttpContext context, IMediator mediator)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && !context.User.HasClaim(c => c.Type == "cirth:user_id"))
        {
            var oid = context.User.FindFirstValue("oid") ?? context.User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");
            var email = context.User.FindFirstValue("preferred_username") ?? context.User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var name = context.User.FindFirstValue("name") ?? context.User.FindFirstValue(ClaimTypes.Name) ?? email;

            if (oid is not null && Guid.TryParse(oid, out var entraOid))
            {
                var cmd = new ProvisionUserCommand(
                    entraOid, email, name,
                    Guid.Parse(EntraTenantId));

                var result = await mediator.Send(cmd);
                if (result.IsSuccess)
                {
                    var identity = context.User.Identity as ClaimsIdentity;
                    identity?.AddClaim(new Claim("cirth:user_id", result.Value!.UserId.ToString()));
                    identity?.AddClaim(new Claim("cirth:tenant_id", result.Value!.TenantId.ToString()));
                    identity?.AddClaim(new Claim(ClaimTypes.Role, result.Value!.Role));
                }
            }
        }

        await next(context);
    }
}
