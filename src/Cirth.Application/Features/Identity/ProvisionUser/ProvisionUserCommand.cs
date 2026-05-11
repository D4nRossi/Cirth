using Cirth.Application.Common.Ports;
using Cirth.Domain.Quotas;
using Cirth.Domain.Tenants;
using Cirth.Domain.Users;
using Cirth.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cirth.Application.Features.Identity.ProvisionUser;

/// <summary>
/// Called on first OIDC login. Creates Tenant + User(Admin) if first user, otherwise User(User).
/// </summary>
public sealed record ProvisionUserCommand(
    Guid EntraObjectId,
    string Email,
    string DisplayName,
    Guid TenantId) : IRequest<Result<ProvisionUserResult>>;

public sealed record ProvisionUserResult(Guid UserId, Guid TenantId, string Role);

internal sealed class ProvisionUserCommandHandler(
    IUserRepository userRepo,
    ITenantRepository tenantRepo,
    IUserQuotaRepository quotaRepo,
    IQueryDbContext db,
    IUnitOfWork uow) : IRequestHandler<ProvisionUserCommand, Result<ProvisionUserResult>>
{
    public async Task<Result<ProvisionUserResult>> Handle(ProvisionUserCommand cmd, CancellationToken ct)
    {
        // Idempotent: if user already exists, return existing
        var existing = await userRepo.GetByEntraObjectIdAsync(cmd.EntraObjectId, ct);
        if (existing is not null)
            return Result<ProvisionUserResult>.Success(
                new(existing.Id.Value, existing.TenantId.Value, existing.Role.ToString()));

        var tenantId = new TenantId(cmd.TenantId);

        // Check if tenant exists; if not, create it (first user = admin)
        var tenant = await tenantRepo.GetByIdAsync(tenantId, ct);
        UserRole role;
        if (tenant is null)
        {
            tenant = Tenant.Create(cmd.DisplayName + "'s Knowledge Base");
            await tenantRepo.AddAsync(tenant, ct);
            tenantId = tenant.Id;
            role = UserRole.Admin;
        }
        else
        {
            var anyUser = await db.Users.AnyAsync(u => u.TenantId.Value == tenantId.Value, ct);
            role = anyUser ? UserRole.User : UserRole.Admin;
        }

        var user = User.Provision(tenantId, cmd.EntraObjectId, cmd.Email, cmd.DisplayName, role);
        await userRepo.AddAsync(user, ct);

        var quota = UserQuota.CreateDefault(user.Id, tenantId);
        await quotaRepo.AddAsync(quota, ct);

        await uow.CommitAsync(ct);
        return Result<ProvisionUserResult>.Success(new(user.Id.Value, tenantId.Value, role.ToString()));
    }
}
