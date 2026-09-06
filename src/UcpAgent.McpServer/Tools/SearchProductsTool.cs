using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace UcpAgent.McpServer.Tools;

[McpServerToolType]
public sealed class SearchProductsTool(IHttpClientFactory httpFactory)
{
    [McpServerTool(Name = "search_products")]
    [Description("Busca produtos em mÃºltiplos catÃ¡logos (MercadoLivre, VTEX, OpenFoodFacts). Retorna lista ranqueada por disponibilidade e menor preÃ§o.")]
    public async Task<string> SearchAsync(
        [Description("Termo de busca. Ex: 'notebook gamer', 'leite integral', 'tÃªnis running'")] string query,
        [Description("NÃºmero mÃ¡ximo de resultados por fonte (padrÃ£o: 5)")] int limit = 5)
    {
        var client = httpFactory.CreateClient("CompraApi");
        var response = await client.GetAsync($"/api/products/search?query={Uri.EscapeDataString(query)}&limit={limit}");

        if (!response.IsSuccessStatusCode)
            return $"Erro ao buscar produtos: {response.StatusCode}";

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json);
        var produtos = result.GetProperty("data");
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Encontrei {produtos.GetArrayLength()} produtos para '{query}':\n");

        int i = 1;
        foreach (var p in produtos.EnumerateArray())
        {
            sb.AppendLine($"{i}. **{p.GetProperty("name").GetString()}**");
            sb.AppendLine($"   - ID    : {p.GetProperty("id").GetString()}");
            sb.AppendLine($"   - Fonte : {p.GetProperty("source").GetString()}");
            sb.AppendLine($"   - PreÃ§o : R$ {p.GetProperty("price").GetDecimal():N2}");
            sb.AppendLine($"   - DisponÃ­vel: {(p.GetProperty("available").GetBoolean() ? "Sim" : "NÃ£o")}");
            if (p.TryGetProperty("url", out var url))
                sb.AppendLine($"   - Link  : {url.GetString()}");
            sb.AppendLine();
            i++;
        }
        return sb.ToString();
    }
}
