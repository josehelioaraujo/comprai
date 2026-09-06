using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace UcpAgent.McpServer.Tools;

[McpServerToolType]
public sealed class IntentTool(IHttpClientFactory httpFactory)
{
    [McpServerTool(Name = "process_intent")]
    [Description("""
        Processa mensagem em linguagem natural e executa a aÃ§Ã£o correta.
        Use quando o usuÃ¡rio escrever de forma conversacional:
        'quero comprar um notebook', 'adiciona ao carrinho', 'finalizar pedido',
        'qual o status do meu pedido?'. Delega ao IntentRouterService (Fase 12).
        """)]
    public async Task<string> ProcessIntentAsync(
        [Description("Mensagem do usuÃ¡rio em linguagem natural")] string text,
        [Description("ID do usuÃ¡rio ou sessÃ£o (necessÃ¡rio para carrinho/checkout)")] string? userId = null)
    {
        var client  = httpFactory.CreateClient("CompraApi");
        var payload = JsonSerializer.Serialize(new { text, userId });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/intent", content);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return $"Erro ao processar intenÃ§Ã£o: {response.StatusCode}";

        var result = JsonSerializer.Deserialize<JsonElement>(json);
        var intent = result.GetProperty("intent").GetString();
        var data   = result.GetProperty("data");

        return $"""
                ðŸ§  IntenÃ§Ã£o detectada: **{intent}**

                {data}
                """;
    }
}
