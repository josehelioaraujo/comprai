using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Events;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Cart;

public sealed class AddToCartHandler(
    ICartPort cart,
    IEventPublisher events,
    UcpMetrics metrics,
    ICartSnapshotPort? snapshot = null)
    : IRequestHandler<AddToCartCommand, Result<string>>
{
    public async Task<Result<string>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var itemId = await cart.AddItemAsync(request.SessionId, request.Product, request.Quantity, cancellationToken);
        metrics.CartAddTotal.Add(1);

        // Publica evento (fire-and-forget)
        _ = events.PublishAsync(
            UcpTopics.CartItemAdded,
            new CartItemAddedEvent(
                request.SessionId,
                request.Product.Id,
                request.Product.Title,
                request.Quantity,
                request.Product.Price,
                DateTime.UtcNow),
            cancellationToken);

        // Persiste snapshot para recuperação de abandono (fire-and-forget)
        if (snapshot is not null)
            _ = PersistSnapshotAsync(request.SessionId, cart, snapshot, cancellationToken);

        return Result<string>.Ok(itemId);
    }

    private static async Task PersistSnapshotAsync(
        string sessionId, ICartPort cart, ICartSnapshotPort snapshot, CancellationToken ct)
    {
        try
        {
            var items = await cart.GetItemsAsync(sessionId, ct);
            await snapshot.SaveAsync(sessionId, items, ct);
        }
        catch
        {
            // snapshot opcional — nunca bloqueia o add
        }
    }
}
