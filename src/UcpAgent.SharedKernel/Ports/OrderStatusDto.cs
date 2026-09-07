namespace UcpAgent.SharedKernel.Ports;

public record OrderStatusDto(
    string OrderId,
    string Status,
    decimal Total,
    CustomerDto Customer,
    string ItemsJson,
    DateTime CreatedAt);
