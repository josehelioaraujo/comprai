using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Cart;

public sealed class RemoveFromCartCommandHandler(ICartPort cart)
    : IRequestHandler<RemoveFromCartCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        RemoveFromCartCommand request, CancellationToken ct)
    {
        await cart.RemoveItemAsync(request.SessionId, request.ProductId, ct);
        return Result<bool>.Success(true);
    }
}
