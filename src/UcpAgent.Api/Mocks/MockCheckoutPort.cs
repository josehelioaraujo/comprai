using System.Text.Json;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Mocks;

public sealed class MockCheckoutPort(
    IOrderPort orders,
    ICartSnapshotPort cartSnapshot,
    ICartPort cart) : ICheckoutPort
{
    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<CheckoutResultDto> ProcessAsync(
        string sessionId, CustomerDto customer,
        Guid? authenticatedCustomerId = null,
        CancellationToken ct = default)
    {
        var snapshot = await cartSnapshot.GetAsync(sessionId, ct);

        decimal total;
        string  itemsJson;

        if (snapshot is not null)
        {
            total     = snapshot.TotalAmount;
            itemsJson = JsonSerializer.Serialize(snapshot.Items, _jsonOpts);
        }
        else
        {
            // NullCartSnapshotPort ativo (UsarPostgres=false) — lê direto do carrinho em memória
            var items = await cart.GetItemsAsync(sessionId, ct);
            total     = items.Sum(i => i.Subtotal);
            itemsJson = JsonSerializer.Serialize(items, _jsonOpts);
        }

        var orderId = Guid.NewGuid().ToString();
        var dto = new OrderStatusDto(
            OrderId:   orderId,
            Status:    "Pending",
            Total:     total,
            Customer:  customer,
            ItemsJson: itemsJson,
            CreatedAt: DateTime.UtcNow);

        await orders.SaveAsync(dto, ct);
        await orders.SaveSessionOrderAsync(sessionId, orderId, ct);

        return new CheckoutResultDto(orderId, true, null);
    }
}
