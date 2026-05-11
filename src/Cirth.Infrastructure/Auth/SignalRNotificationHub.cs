using Cirth.Application.Common.Ports;
using Cirth.Shared;
using Microsoft.AspNetCore.SignalR;

namespace Cirth.Infrastructure.Auth;

internal sealed class SignalRNotificationHub(IHubContext<CirthHub> hubContext) : INotificationHub
{
    public Task NotifyDocumentIndexedAsync(TenantId tenantId, UserId userId, Guid documentId, string title, CancellationToken ct)
        => hubContext.Clients.Group(userId.Value.ToString())
            .SendAsync("DocumentIndexed", new { documentId, title }, cancellationToken: ct);

    public Task NotifyDocumentFailedAsync(TenantId tenantId, UserId userId, Guid documentId, string title, string reason, CancellationToken ct)
        => hubContext.Clients.Group(userId.Value.ToString())
            .SendAsync("DocumentFailed", new { documentId, title, reason }, cancellationToken: ct);
}
