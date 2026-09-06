namespace UcpAgent.SharedKernel.Ports;

public interface IOrderPort
{
    Task<OrderStatusDto?> GetStatusAsync(string orderId, CancellationToken cancellationToken = default);
}

public record OrderStatusDto(
    string OrderId,
    string Status,
    decimal Total,
    CustomerDto? Customer,
    string? ItemsJson,
    DateTime CreatedAt,
    string? TrackingCode = null);
