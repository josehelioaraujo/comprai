using System.Text.Json;
using UcpAgent.SharedKernel.Events;
using UcpAgent.SharedKernel.Ports;
using UcpAgent.Infrastructure.Orders;

namespace UcpAgent.Infrastructure.Checkout;

public sealed class RedisCheckoutAdapter(
    ICartPort cart,
    RedisOrderAdapter orders,
    IEventPublisher events,
    INotificationPublisher notifications) : ICheckoutPort
{
    public async Task<CheckoutResultDto> ProcessAsync(
        string sessionId, CustomerDto customer, CancellationToken ct = default)
    {
        var items = await cart.GetItemsAsync(sessionId, ct);
        if (items.Count == 0)
            return new CheckoutResultDto(string.Empty, false, "Carrinho vazio");

        var orderId = $"ORDER-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        var total   = items.Sum(i => i.Subtotal);

        var order = new OrderStatusDto(
            orderId, "Pending", total, customer,
            JsonSerializer.Serialize(items), DateTime.UtcNow);

        await orders.SaveAsync(order);
        await cart.ClearAsync(sessionId, ct);

        // Kafka — evento de domínio (fluxo UCP)
        await events.PublishAsync(
            UcpTopics.OrderCreated,
            new OrderCreatedEvent(orderId, sessionId, total, items.Count, DateTime.UtcNow),
            ct);

        // RabbitMQ — notificação ao usuário (email)
        _ = notifications.PublishAsync(
            NotificationQueues.OrderConfirmation,
            new OrderConfirmationNotification(
                orderId,
                sessionId,
                customer.Email,
                customer.Name,
                total,
                items.Count,
                DateTime.UtcNow),
            ct);

        return new CheckoutResultDto(orderId, true, null);
    }
}
