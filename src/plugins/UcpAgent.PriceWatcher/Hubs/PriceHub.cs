using Microsoft.AspNetCore.SignalR;

namespace UcpAgent.PriceWatcher.Hubs;

public sealed class PriceHub : Hub
{
    public async Task JoinSession(string sessionId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

    public async Task LeaveSession(string sessionId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
}
