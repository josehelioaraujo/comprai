using System.Diagnostics.CodeAnalysis;

namespace UcpAgent.SharedKernel.Events;

/// <summary>
/// Payload da notificação de atualização de status — publicado no RabbitMQ.
/// </summary>
[ExcludeFromCodeCoverage]
public record OrderStatusNotification(
    string OrderId,
    string CustomerEmail,
    string OldStatus,
    string NewStatus,
    DateTime UpdatedAt);
