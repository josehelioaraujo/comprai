using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Cart;

public sealed class GetCartQueryHandler(ICartPort cart)
    : IRequestHandler<GetCartQuery, Result<IReadOnlyList<CartItemDto>>>
{
    public async Task<Result<IReadOnlyList<CartItemDto>>> Handle(
        GetCartQuery request, CancellationToken ct)
    {
        var items = await cart.GetItemsAsync(request.SessionId, ct);
        return Result<IReadOnlyList<CartItemDto>>.Ok(items);
    }
}