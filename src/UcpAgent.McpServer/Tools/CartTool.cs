using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace UcpAgent.McpServer.Tools;

[McpServerToolType]
public sealed class CartTool(IHttpClientFactory httpFactory)
{
    [McpServerTool(Name = "add_to_cart")]
    [Description("Adiciona um produto ao carrinho do usuÃ¡rio.")]
    public async Task<string> AddToCartAsync(
        [Description("ID Ãºnico do produto (obtido via search_products)")] string productId,
        [Description("ID do usuÃ¡rio ou sessÃ£o (ex: 'user-123')")] string userId,
        [Description("Quantidade a adicionar (padrÃ£o: 1)")] int quantity = 1)
    {
        var client = httpFactory.CreateClient("CompraApi");
        var payload = JsonSerializer.Serialize(new { productId, userId, quantity });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/cart/add", content);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return $"Erro ao adicionar ao carrinho: {response.StatusCode} â€” {json}";

        return $"âœ… Produto {productId} adicionado ao carrinho de {userId} (quantidade: {quantity}).";
    }

    [McpServerTool(Name = "view_cart")]
    [Description("Exibe os itens atuais no carrinho do usuÃ¡rio.")]
    public async Task<string> ViewCartAsync(
        [Description("ID do usuÃ¡rio ou sessÃ£o")] string userId)
    {
        var client = httpFactory.CreateClient("CompraApi");
        var response = await client.GetAsync($"/api/cart/{Uri.EscapeDataString(userId)}");

        if (!response.IsSuccessStatusCode)
            return $"Erro ao consultar carrinho: {response.StatusCode}";

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json);
        var itens = result.GetProperty("data").GetProperty("items");

        if (itens.GetArrayLength() == 0)
            return "Carrinho vazio.";

        var sb = new StringBuilder();
        sb.AppendLine($"ðŸ›’ Carrinho de {userId}:\n");
        decimal total = 0;
        foreach (var item in itens.EnumerateArray())
        {
            var nome  = item.GetProperty("name").GetString();
            var qty   = item.GetProperty("quantity").GetInt32();
            var preco = item.GetProperty("price").GetDecimal();
            total += preco * qty;
            sb.AppendLine($"- {nome} Ã— {qty} â€” R$ {preco * qty:N2}");
        }
        sb.AppendLine($"\n**Total: R$ {total:N2}**");
        return sb.ToString();
    }

    [McpServerTool(Name = "remove_from_cart")]
    [Description("Remove um produto do carrinho do usuÃ¡rio.")]
    public async Task<string> RemoveFromCartAsync(
        [Description("ID do produto a remover")] string productId,
        [Description("ID do usuÃ¡rio ou sessÃ£o")] string userId)
    {
        var client = httpFactory.CreateClient("CompraApi");
        var payload = JsonSerializer.Serialize(new { productId, userId });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/cart/remove", content);

        if (!response.IsSuccessStatusCode)
            return $"Erro ao remover item: {response.StatusCode}";

        return $"ðŸ—‘ï¸ Produto {productId} removido do carrinho de {userId}.";
    }
}
