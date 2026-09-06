using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace UcpAgent.McpServer.Tools;

[McpServerToolType]
public sealed class OrderTool(IHttpClientFactory httpFactory)
{
    [McpServerTool(Name = "get_order")]
    [Description("Consulta o status e detalhes de um pedido pelo ID (formato: ORDER-XXXXXXXX).")]
    public async Task<string> GetOrderAsync(
        [Description("ID do pedido (formato: ORDER-XXXXXXXX)")] string orderId)
    {
        var client = httpFactory.CreateClient("CompraApi");
        var response = await client.GetAsync($"/api/orders/{Uri.EscapeDataString(orderId)}");

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return $"Pedido '{orderId}' nÃ£o encontrado.";

        if (!response.IsSuccessStatusCode)
            return $"Erro ao consultar pedido: {response.StatusCode}";

        var json     = await response.Content.ReadAsStringAsync();
        var result   = JsonSerializer.Deserialize<JsonElement>(json);
        var order    = result.GetProperty("data");
        var status   = order.GetProperty("status").GetString();
        var total    = order.GetProperty("total").GetDecimal();
        var criadoEm = order.GetProperty("createdAt").GetString();
        var itens    = order.GetProperty("items");

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"ðŸ“¦ Pedido: **{orderId}**");
        sb.AppendLine($"   Status : {MapStatus(status)}");
        sb.AppendLine($"   Total  : R$ {total:N2}");
        sb.AppendLine($"   Criado : {criadoEm}");
        sb.AppendLine($"\nItens:");
        foreach (var item in itens.EnumerateArray())
        {
            var nome  = item.GetProperty("name").GetString();
            var qty   = item.GetProperty("quantity").GetInt32();
            var preco = item.GetProperty("price").GetDecimal();
            sb.AppendLine($"  - {nome} Ã— {qty} â€” R$ {preco * qty:N2}");
        }
        return sb.ToString();
    }

    private static string MapStatus(string? status) => status switch
    {
        "Pending"   => "â³ Pendente",
        "Confirmed" => "âœ… Confirmado",
        "Shipped"   => "ðŸšš Enviado",
        "Delivered" => "ðŸ“¬ Entregue",
        "Cancelled" => "âŒ Cancelado",
        _           => status ?? "Desconhecido"
    };
}
