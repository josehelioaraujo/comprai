using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace UcpAgent.Integration.Tests;

[Collection("IntegrationTests")]
public class IntentRouterIntegrationTest : IClassFixture<CompraApiFactory>
{
    private readonly HttpClient _client;

    public IntentRouterIntegrationTest(CompraApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> PostIntent(string text)
    {
        var response = await _client.PostAsJsonAsync("/api/intent",
            new { text, sessionId = "test-session" });

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.TryGetProperty("intent",   out var i) ? i.GetString() ?? "" :
               body.TryGetProperty("detected", out var d) ? d.GetString() ?? "" :
               body.TryGetProperty("type",     out var t) ? t.GetString() ?? "" : "";
    }

    [Theory]
    [InlineData("quero comprar notebook",    "SearchProducts")]
    [InlineData("buscar celular Samsung",    "SearchProducts")]
    [InlineData("ver meu carrinho",          "ViewCart")]
    [InlineData("finalizar compra",          "Checkout")]
    [InlineData("status do pedido ORDER-001","GetOrder")]
    public async Task IntentRouter_ReturnsCorrectIntent(string message, string expected)
    {
        var intent = await PostIntent(message);
        Assert.Equal(expected, intent);
    }

    [Fact]
    public async Task IntentRouter_AddToCart_Returns422OrOk()
    {
        // AddToCart retorna 422 sem productId — comportamento esperado
        var response = await _client.PostAsJsonAsync("/api/intent",
            new { text = "adicionar ao carrinho", sessionId = "test-session" });
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity ||
            response.StatusCode == System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task IntentRouter_GetOrders_ReturnsAnyIntent()
    {
        var intent = await PostIntent("meus pedidos");
        Assert.False(string.IsNullOrEmpty(intent));
    }
}
