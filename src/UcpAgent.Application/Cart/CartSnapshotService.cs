using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Cart;

public sealed class CartSnapshotService(ICartSnapshotPort port) : ICartSnapshotService
{
    public async Task PersistAsync(string sessionId, ICartPort cart, CancellationToken ct = default)
    {
        try
        {
            var items = await cart.GetItemsAsync(sessionId, ct);
            if (items is null || items.Count == 0)
                await port.DeleteAsync(sessionId, ct);
            else
                await port.SaveAsync(sessionId, items, ct);
        }
        catch { }
    }

    public Task DeleteAsync(string sessionId, CancellationToken ct = default)
    {
        try { return port.DeleteAsync(sessionId, ct); }
        catch { return Task.CompletedTask; }
    }

    public Task<CartSnapshotDto?> GetAsync(string sessionId, CancellationToken ct = default)
        => port.GetAsync(sessionId, ct);
}
