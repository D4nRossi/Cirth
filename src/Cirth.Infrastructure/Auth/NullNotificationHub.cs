using Cirth.Application.Common.Ports;
using Cirth.Shared;

namespace Cirth.Infrastructure.Auth;

internal sealed class NullNotificationHub : INotificationHub
{
    public Task NotifyDocumentIndexedAsync(TenantId tenantId, UserId userId, Guid documentId, string title, CancellationToken ct)
        => Task.CompletedTask;

    public Task NotifyDocumentFailedAsync(TenantId tenantId, UserId userId, Guid documentId, string title, string reason, CancellationToken ct)
        => Task.CompletedTask;
}
