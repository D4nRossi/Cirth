using Cirth.Application.Common.Ports;
using Cirth.Shared;
using System.Security.Claims;

namespace Cirth.Mcp.Auth;

public sealed class ApiKeyAuthMiddleware(RequestDelegate next)
{
    private const string ApiKeyHeader = "X-Cirth-Api-Key";

    public async Task InvokeAsync(HttpContext context, IApiKeyRepository apiKeyRepo, IApiKeyHasher hasher,
        IUserRepository userRepo)
    {
        if (context.Request.Headers.TryGetValue(ApiKeyHeader, out var keyValues))
        {
            var plainKey = keyValues.FirstOrDefault();
            if (!string.IsNullOrEmpty(plainKey))
            {
                var hash = hasher.Hash(plainKey);
                var apiKey = await apiKeyRepo.GetByHashAsync(hash);
                if (apiKey is not null && apiKey.IsValid())
                {
                    var user = await userRepo.GetByIdAsync(apiKey.UserId);
                    if (user is not null)
                    {
                        var identity = new ClaimsIdentity([
                            new Claim("cirth:user_id", user.Id.Value.ToString()),
                            new Claim("cirth:tenant_id", user.TenantId.Value.ToString()),
                            new Claim(ClaimTypes.Role, user.Role.ToString())
                        ], "ApiKey");
                        context.User = new ClaimsPrincipal(identity);
                        apiKey.RecordUsage();
                    }
                }
            }
        }

        await next(context);
    }
}
