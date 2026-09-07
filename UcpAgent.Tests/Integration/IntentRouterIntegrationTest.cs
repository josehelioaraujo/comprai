using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace UcpAgent.Tests.Integration;

[Collection("IntegrationTests")]
public class IntentRouterIntegrationTest : IClassFixture<CompraApiFactory>
{
    private readonly HttpClient _client;

    public IntentRouterIntegrationTest(CompraApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> PostIntent(string message)
    {
        var payload = new { message };
        var response = await _client.PostAsJsonAsync("/api/intent", payload);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("intent").GetString() ?? "";
    }

    [Theory]
    [InlineData("quero comprar notebook", "search")]
    [InlineData("buscar celular Samsung", "search")]
    [InlineData("adicionar ao carrinho", "add_to_cart")]
    [InlineData("ver meu carrinho", "get_cart")]
    [InlineData("finalizar compra", "checkout")]
    [InlineData("meus pedidos", "get_orders")]
    [InlineData("status do pedido ORDER-001", "get_order_status")]
    public async Task IntentRouter_ReturnsCorrectIntent(string message, string expectedIntent)
    {
        var intent = await PostIntent(message);
        Assert.Equal(expectedIntent, intent);
    }

    [Fact]
    public async Task IntentRouter_UnknownMessage_ReturnsUnknown()
    {
        var intent = await PostIntent("xxxxxxxx mensagem inválida xxxxxxxx");
        Assert.Equal("unknown", intent);
    }
}
