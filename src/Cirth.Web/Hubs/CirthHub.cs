using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Cirth.Web.Hubs;

[Authorize]
public sealed class CirthHub : Hub
{
    // Groups are keyed by userId so only the relevant user receives notifications.
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("cirth:user_id")?.Value;
        if (userId is not null)
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }
}
