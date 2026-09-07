using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Orders;

public interface IOrderService
{
    Task<Result<OrderStatusDto?>> GetByIdAsync(
        string orderId, CancellationToken ct = default);

    Task<Result<OrderStatusDto?>> GetBySessionAsync(
        string sessionId, CancellationToken ct = default);
}
