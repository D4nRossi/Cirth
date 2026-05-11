using Microsoft.AspNetCore.SignalR;

namespace Cirth.Infrastructure.Auth;

// Concrete Hub required by IHubContext<THub>. Clients connect here for push notifications.
public sealed class CirthHub : Hub { }
