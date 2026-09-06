using UcpAgent.SharedKernel;

namespace UcpAgent.Application.Orders;

public interface IOrderService
{
    Task<Result<object>> GetByIdAsync(string orderId, CancellationToken ct = default);
    Task<Result<object>> GetBySessionAsync(string sessionId, CancellationToken ct = default);
}
