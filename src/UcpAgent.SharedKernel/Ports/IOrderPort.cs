namespace UcpAgent.SharedKernel.Ports;

public interface IOrderPort
{
    Task<OrderStatusDto?> GetStatusAsync(string orderId, CancellationToken cancellationToken = default);
    Task<string?> GetOrderIdBySessionAsync(string sessionId, CancellationToken ct = default);
    Task SaveAsync(OrderStatusDto order, CancellationToken ct = default);
    Task SaveSessionOrderAsync(string sessionId, string orderId, CancellationToken ct = default);
}
