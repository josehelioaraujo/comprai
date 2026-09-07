using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;
using UcpAgent.Application.Order;

namespace UcpAgent.Application.Orders;

public sealed class OrderService(ISender mediator) : IOrderService
{
    public Task<Result<OrderStatusDto?>> GetByIdAsync(string orderId, CancellationToken ct = default)
        => mediator.Send(new GetOrderQuery(orderId), ct);

    public Task<Result<OrderStatusDto?>> GetBySessionAsync(string sessionId, CancellationToken ct = default)
        => mediator.Send(new GetOrderBySessionQuery(sessionId), ct);
}