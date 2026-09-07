namespace UcpAgent.SharedKernel.Ports;

public interface IOrderPort
{
    Task<OrderStatusDto?> GetStatusAsync(string orderId, CancellationToken cancellationToken = default);
    Task<string?> GetOrderIdBySessionAsync(string sessionId, CancellationToken ct = default);
}
