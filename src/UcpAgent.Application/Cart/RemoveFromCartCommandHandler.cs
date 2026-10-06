using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Cart;

public sealed class RemoveFromCartCommandHandler(
    ICartPort cart,
    ICartSnapshotPort? snapshot = null)
    : IRequestHandler<RemoveFromCartCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        RemoveFromCartCommand request, CancellationToken ct)
    {
        await cart.RemoveItemAsync(request.SessionId, request.ProductId, ct);

        if (snapshot is not null)
            _ = PersistSnapshotAsync(request.SessionId, cart, snapshot, ct);

        return Result<bool>.Ok(true);
    }

    private static async Task PersistSnapshotAsync(
        string sessionId, ICartPort cart, ICartSnapshotPort snapshot, CancellationToken ct)
    {
        try
        {
            var items = await cart.GetItemsAsync(sessionId, ct);
            if (items is null || items.Count == 0)
                await snapshot.DeleteAsync(sessionId, ct);
            else
                await snapshot.SaveAsync(sessionId, items, ct);
        }
        catch { }
    }
}
