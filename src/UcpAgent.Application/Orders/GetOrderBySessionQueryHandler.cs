using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Orders;

public sealed class GetOrderBySessionQueryHandler(IOrderPort orders)
    : IRequestHandler<GetOrderBySessionQuery, Result<OrderStatusDto?>>
{
    public async Task<Result<OrderStatusDto?>> Handle(
        GetOrderBySessionQuery request, CancellationToken ct)
    {
        var orderId = await orders.GetOrderIdBySessionAsync(request.SessionId, ct);
        if (orderId is null)
            return Result<OrderStatusDto?>.Ok(null);

        var order = await orders.GetStatusAsync(orderId, ct);
        return Result<OrderStatusDto?>.Ok(order);
    }
}