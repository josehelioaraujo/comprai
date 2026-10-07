using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Events;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Cart;

public sealed class AddToCartHandler(
    ICartPort cart,
    IEventPublisher events,
    UcpMetrics metrics,
    ICartSnapshotService? snapshotService = null)
    : IRequestHandler<AddToCartCommand, Result<string>>
{
    public async Task<Result<string>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var itemId = await cart.AddItemAsync(request.SessionId, request.Product, request.Quantity, cancellationToken);
        metrics.CartAddTotal.Add(1);

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

        if (snapshotService is not null)
            _ = snapshotService.PersistAsync(request.SessionId, cart, cancellationToken);

        return Result<string>.Ok(itemId);
    }
}
