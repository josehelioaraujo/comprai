using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Cart;

public sealed class AddToCartHandler(ICartPort cart)
    : IRequestHandler<AddToCartCommand, Result<string>>
{
    public async Task<Result<string>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var itemId = await cart.AddItemAsync(request.SessionId, request.Product, request.Quantity, cancellationToken);
        return Result<string>.Ok(itemId);
    }
}
