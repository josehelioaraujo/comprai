namespace UcpAgent.SharedKernel.Events;

/// <summary>
/// Filas RabbitMQ de notificações ao usuário.
/// </summary>
public static class NotificationQueues
{
    public const string OrderConfirmation = "notifications.order.confirmation"; // pedido criado
    public const string OrderStatusUpdate = "notifications.order.status";       // status alterado
}
