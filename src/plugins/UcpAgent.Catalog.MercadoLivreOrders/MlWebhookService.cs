using System.Text.Json;
using System.Text.Json.Serialization;

namespace UcpAgent.Catalog.MercadoLivreOrders;

public sealed class MlWebhookService(MlOrdersService ordersService)
{
    public async Task<MlWebhookResult> ProcessAsync(MlWebhookPayload payload, CancellationToken ct = default)
    {
        if (payload.Topic != "orders_v2")
            return new MlWebhookResult(false, $"Topico ignorado: {payload.Topic}");

        // Extrai order ID da URL do resource
        // Exemplo: /orders/12345678
        var parts   = payload.Resource.Split('/');
        var orderId = parts.LastOrDefault();

        if (string.IsNullOrEmpty(orderId))
            return new MlWebhookResult(false, "Order ID nao encontrado no resource");

        var order = await ordersService.GetOrderAsync(orderId, ct);
        if (order is null)
            return new MlWebhookResult(false, $"Pedido {orderId} nao encontrado");

        // Aqui pode publicar evento, atualizar cache, notificar etc.
        return new MlWebhookResult(true, $"Pedido {orderId} processado - status: {order.Status}");
    }
}

public record MlWebhookPayload(
    [property: JsonPropertyName("_id")]      string Id,
    [property: JsonPropertyName("resource")] string Resource,
    [property: JsonPropertyName("user_id")]  long   UserId,
    [property: JsonPropertyName("topic")]    string Topic,
    [property: JsonPropertyName("received")] string Received
);

public record MlWebhookResult(bool Success, string Message);

