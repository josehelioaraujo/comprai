using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Order;

public sealed class GetOrderHandler(IOrderPort orders)
    : IRequestHandler<GetOrderQuery, Result<OrderStatusDto?>>
{
    public async Task<Result<OrderStatusDto?>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var status = await orders.GetStatusAsync(request.OrderId, cancellationToken);
        return Result<OrderStatusDto?>.Ok(status);
    }
}
