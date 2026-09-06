using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace UcpAgent.McpServer.Tools;

[McpServerToolType]
public sealed class CheckoutTool(IHttpClientFactory httpFactory)
{
    [McpServerTool(Name = "checkout")]
    [Description("Finaliza a compra do carrinho e gera um pedido. Retorna o ID do pedido gerado.")]
    public async Task<string> CheckoutAsync(
        [Description("ID do usuÃ¡rio ou sessÃ£o")] string userId,
        [Description("EndereÃ§o de entrega completo")] string? enderecoEntrega = null,
        [Description("Forma de pagamento: 'pix', 'cartao', 'boleto' (padrÃ£o: 'pix')")] string pagamento = "pix")
    {
        var client = httpFactory.CreateClient("CompraApi");
        var payload = JsonSerializer.Serialize(new
        {
            userId,
            enderecoEntrega = enderecoEntrega ?? "A confirmar",
            pagamento
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/checkout", content);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return $"Erro no checkout: {response.StatusCode} â€” {json}";

        var result  = JsonSerializer.Deserialize<JsonElement>(json);
        var orderId = result.GetProperty("data").GetProperty("orderId").GetString();
        var total   = result.GetProperty("data").GetProperty("total").GetDecimal();

        return $"""
                âœ… Pedido criado com sucesso!

                ðŸ“¦ ID do Pedido : {orderId}
                ðŸ’³ Pagamento    : {pagamento.ToUpper()}
                ðŸ’° Total        : R$ {total:N2}
                ðŸ“ Entrega      : {enderecoEntrega ?? "A confirmar"}

                Use `get_order` com o ID acima para acompanhar o status.
                """;
    }
}
