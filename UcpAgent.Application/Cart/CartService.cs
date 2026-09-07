using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Cart;

public sealed class CartService(ISender mediator) : ICartService
{
    public Task<Result<IReadOnlyList<CartItemDto>>> GetCartAsync(
        string sessionId, CancellationToken ct = default)
        => mediator.Send(new GetCartQuery(sessionId), ct);

    public Task<Result<string>> AddItemAsync(
        string sessionId, ProductDto product, int quantity = 1, CancellationToken ct = default)
        => mediator.Send(new AddToCartCommand(sessionId, product, quantity), ct);

    public Task<Result<bool>> RemoveItemAsync(
        string sessionId, string productId, CancellationToken ct = default)
        => mediator.Send(new RemoveFromCartCommand(sessionId, productId), ct);
}
