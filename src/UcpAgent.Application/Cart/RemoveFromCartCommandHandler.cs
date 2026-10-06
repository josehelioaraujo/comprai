using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Cart;

public sealed class RemoveFromCartCommandHandler(
    ICartPort cart,
    ICartSnapshotService? snapshotService = null)
    : IRequestHandler<RemoveFromCartCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        RemoveFromCartCommand request, CancellationToken ct)
    {
        await cart.RemoveItemAsync(request.SessionId, request.ProductId, ct);

        if (snapshotService is not null)
            _ = snapshotService.PersistAsync(request.SessionId, cart, ct);

        return Result<bool>.Ok(true);
    }
}
