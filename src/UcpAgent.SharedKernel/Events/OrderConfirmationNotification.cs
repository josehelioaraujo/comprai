using System.Diagnostics.CodeAnalysis;

namespace UcpAgent.SharedKernel.Events;

/// <summary>
/// Payload da notificação de confirmação de pedido — publicado no RabbitMQ.
/// Quando Resend for integrado, o NotificationWorker usa esses dados para montar o email.
/// </summary>
[ExcludeFromCodeCoverage]
public record OrderConfirmationNotification(
    string OrderId,
    string SessionId,
    string CustomerEmail,
    string CustomerName,
    decimal Total,
    int ItemCount,
    DateTime CreatedAt);
