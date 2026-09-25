using MediatR;
using UcpAgent.Api;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Cart;

public sealed class AddToCartHandler(ICartPort cart, UcpMetrics metrics)
    : IRequestHandler<AddToCartCommand, Result<string>>
{
    public async Task<Result<string>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var itemId = await cart.AddItemAsync(request.SessionId, request.Product, request.Quantity, cancellationToken);
        metrics.CartAddTotal.Add(1);
        return Result<string>.Ok(itemId);
    }
}
