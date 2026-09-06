namespace UcpAgent.SharedKernel.Events;

public record OrderStatusUpdatedEvent(
    string OrderId,
    string OldStatus,
    string NewStatus,
    string? TrackingCode,
    DateTime UpdatedAt);
