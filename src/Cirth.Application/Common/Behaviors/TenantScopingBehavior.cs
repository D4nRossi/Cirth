using Cirth.Application.Common.Ports;
using MediatR;

namespace Cirth.Application.Common.Behaviors;

/// <summary>
/// Garante que o AppDbContext está configurado com o TenantId do request antes de qualquer handler.
/// </summary>
internal sealed class TenantScopingBehavior<TRequest, TResponse>(
    ITenantProvider tenantProvider,
    IDbContextAccessor dbContextAccessor)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        dbContextAccessor.SetTenant(tenantProvider.CurrentTenantId);
        return await next();
    }
}
