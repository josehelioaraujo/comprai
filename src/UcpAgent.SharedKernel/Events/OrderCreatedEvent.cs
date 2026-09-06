namespace UcpAgent.SharedKernel.Events;

public record OrderCreatedEvent(
    string OrderId,
    string SessionId,
    decimal Total,
    int ItemCount,
    DateTime CreatedAt);
