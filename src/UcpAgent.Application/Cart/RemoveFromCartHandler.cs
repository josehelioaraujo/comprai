using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Cart;

public sealed class RemoveFromCartHandler(ICartPort cart)
    : IRequestHandler<RemoveFromCartCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
    {
        await cart.RemoveItemAsync(request.SessionId, request.ProductId, cancellationToken);
        return Result<bool>.Ok(true);
    }
}
