using Cirth.Shared;

namespace Cirth.Application.Common.Ports;

public interface INotificationHub
{
    Task NotifyDocumentIndexedAsync(TenantId tenantId, UserId userId, Guid documentId, string title, CancellationToken ct);
    Task NotifyDocumentFailedAsync(TenantId tenantId, UserId userId, Guid documentId, string title, string reason, CancellationToken ct);
}
