using System.Text.Json;
using UcpAgent.Infrastructure.Orders;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Checkout;

public sealed class RedisCheckoutAdapter(
    ICartPort cart,
    RedisOrderAdapter orders) : ICheckoutPort
{
    public async Task<CheckoutResultDto> ProcessAsync(
        string sessionId, CustomerDto customer, CancellationToken ct = default)
    {
        var items = await cart.GetItemsAsync(sessionId, ct);

        if (items.Count == 0)
            return new CheckoutResultDto(string.Empty, false, "Carrinho vazio");

        var orderId  = $"ORDER-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        var total    = items.Sum(i => i.Subtotal);
        var itemsJson = JsonSerializer.Serialize(items);

        var order = new OrderStatusDto(
            OrderId:   orderId,
            Status:    "Pending",
            Total:     total,
            Customer:  customer,
            ItemsJson: itemsJson,
            CreatedAt: DateTime.UtcNow);

        await orders.SaveAsync(order);
        await cart.ClearAsync(sessionId, ct);

        return new CheckoutResultDto(orderId, true, null);
    }
}
