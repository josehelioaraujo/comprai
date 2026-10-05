using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UcpAgent.SharedKernel.Events;
using UcpAgent.SharedKernel.Ports;
using UcpAgent.Infrastructure.Orders;
using UcpAgent.Infrastructure.Persistence.Repositories;

namespace UcpAgent.Infrastructure.Checkout;

public sealed class RedisCheckoutAdapter(
    ICartPort cart,
    RedisOrderAdapter orders,
    IEventPublisher events,
    INotificationPublisher notifications,
    IConfiguration configuration,
    ILogger<RedisCheckoutAdapter> logger,
    CustomerRepository? customerRepo = null,
    OrderRepository? orderRepo = null) : ICheckoutPort
{
    // GetValue<bool> requer Binder — usar comparação de string direta
    private readonly bool _usarPostgres =
        string.Equals(configuration["Features:UsarPostgres"], "true",
            StringComparison.OrdinalIgnoreCase);

    public async Task<CheckoutResultDto> ProcessAsync(
        string sessionId, CustomerDto customer, CancellationToken ct = default)
    {
        var items = await cart.GetItemsAsync(sessionId, ct);
        if (items.Count == 0)
            return new CheckoutResultDto(string.Empty, false, "Carrinho vazio");

        var orderId = $"ORDER-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        var total   = items.Sum(i => i.Subtotal);

        // ── F1: Persistência PostgreSQL ──────────────────────────────────
        if (_usarPostgres && customerRepo is not null && orderRepo is not null)
        {
            try
            {
                // Etapa 1 — Upsert cliente
                var customerId = await customerRepo.UpsertAsync(
                    customer.Name, customer.Email, customer.Phone,
                    channel: "web", ct);
                logger.LogInformation("[F1] Customer upserted: {CustomerId}", customerId);

                // Etapa 2 — Salva order + items + order_outbox em 1 TX
                var order = new Domain.Entities.Order
                {
                    Id         = orderId,
                    SessionId  = sessionId,
                    CustomerId = customerId,
                    Status     = Domain.Enums.OrderStatus.Pending,
                    CreatedAt  = DateTime.UtcNow,
                    UpdatedAt  = DateTime.UtcNow
                };

                foreach (var item in items)
                    order.AddItemFromCart(item);

                await orderRepo.SaveAsync(order, ct);
                logger.LogInformation("[F1] Order saved: {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[F1] Falha ao persistir no BD — continuando com Redis");
            }
        }

        // ── Redis: source of truth operacional (cache quente / fallback) ─
        var redisOrder = new OrderStatusDto(
            orderId, "Pending", total, customer,
            JsonSerializer.Serialize(items), DateTime.UtcNow);

        await orders.SaveAsync(redisOrder);
        await cart.ClearAsync(sessionId, ct);

        // Kafka — evento de domínio
        await events.PublishAsync(
            UcpTopics.OrderCreated,
            new OrderCreatedEvent(orderId, sessionId, total, items.Count, DateTime.UtcNow),
            ct);

        // RabbitMQ — notificação ao usuário
        _ = notifications.PublishAsync(
            NotificationQueues.OrderConfirmation,
            new OrderConfirmationNotification(
                orderId, sessionId, customer.Email, customer.Name,
                total, items.Count, DateTime.UtcNow),
            ct);

        return new CheckoutResultDto(orderId, true, null);
    }
}
