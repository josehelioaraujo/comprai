using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UcpAgent.SharedKernel.Events;
using UcpAgent.SharedKernel.Ports;
using UcpAgent.Infrastructure.Orders;
using UcpAgent.Infrastructure.Persistence.Repositories;
using UcpAgent.Infrastructure.Fulfillment;

namespace UcpAgent.Infrastructure.Checkout;

public sealed class RedisCheckoutAdapter(
    ICartPort cart,
    RedisOrderAdapter orders,
    IEventPublisher events,
    INotificationPublisher notifications,
    IConfiguration configuration,
    ILogger<RedisCheckoutAdapter> logger,
    FulfillmentSimulator? fulfillmentSimulator = null,
    CustomerRepository?   customerRepo         = null,
    OrderRepository?      orderRepo            = null) : ICheckoutPort
{
    private readonly bool _usarPostgres =
        string.Equals(configuration["Features:UsarPostgres"], "true",
            StringComparison.OrdinalIgnoreCase);

    public async Task<CheckoutResultDto> ProcessAsync(
        string sessionId, CustomerDto customer,
        Guid? authenticatedCustomerId = null,
        CancellationToken ct = default)
    {
        var items = await cart.GetItemsAsync(sessionId, ct);
        if (items.Count == 0)
            return new CheckoutResultDto(string.Empty, false, "Carrinho vazio");

        // orderId = UUID puro — compatível com order.id (UUID) no PostgreSQL
        var orderId = Guid.NewGuid().ToString();
        var total   = items.Sum(i => i.Subtotal);

        // ── F1: Persistência PostgreSQL ──────────────────────────────────────
        if (_usarPostgres && customerRepo is not null && orderRepo is not null)
        {
            try
            {
                // V057-F2: usa customer_id do JWT se autenticado, senão upsert por email
                var customerId = authenticatedCustomerId
                    ?? await customerRepo.UpsertAsync(
                           customer.Name, customer.Email, customer.Phone,
                           channel: "web", ct);
                logger.LogInformation("[F1] Customer resolved: {CustomerId} (auth={IsAuth})",
                    customerId, authenticatedCustomerId.HasValue);

                var order = new Domain.Entities.Order
                {
                    Id         = orderId,
                    SessionId  = sessionId,
                    CustomerId = customerId,
                    Status     = Domain.Enums.OrderStatus.Pending,
                    CreatedAt  = DateTime.UtcNow,
                    UpdatedAt  = DateTime.UtcNow,
                    // endereço snapshot
                    ShippingZip        = customer.Cep?.Replace("-", ""),
                    ShippingStreet     = customer.Street,
                    ShippingNumber     = customer.Number,
                    ShippingComplement = customer.Complement,
                    ShippingCity       = customer.City,
                    ShippingState      = customer.State,
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

        // ── Redis: source of truth operacional ──────────────────────────────
        var redisOrder = new OrderStatusDto(
            orderId, "Pending", total, customer,
            JsonSerializer.Serialize(items), DateTime.UtcNow);

        await orders.SaveAsync(redisOrder);
        await orders.SaveSessionOrderAsync(sessionId, orderId, ct);
        await cart.ClearAsync(sessionId, ct);

        // Kafka — evento de domínio
        await events.PublishAsync(
            UcpTopics.OrderCreated,
            new OrderCreatedEvent(orderId, sessionId, total, items.Count, DateTime.UtcNow),
            ct);

        // RabbitMQ — notificação
        _ = notifications.PublishAsync(
            NotificationQueues.OrderConfirmation,
            new OrderConfirmationNotification(
                orderId, sessionId, customer.Email, customer.Name,
                total, items.Count, DateTime.UtcNow),
            ct);

        // Enqueue fulfillment simulation (se ativo)
        fulfillmentSimulator?.Enqueue(orderId);

        return new CheckoutResultDto(orderId, true, null);
    }
}
