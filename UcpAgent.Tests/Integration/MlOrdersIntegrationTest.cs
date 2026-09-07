using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace UcpAgent.Tests.Integration;

[Collection("IntegrationTests")]
public class MlOrdersIntegrationTest : IClassFixture<CompraApiFactory>
{
    private readonly HttpClient _client;
    private readonly string _mlToken;

    public MlOrdersIntegrationTest(CompraApiFactory factory)
    {
        _client = factory.CreateClient();
        _mlToken = Environment.GetEnvironmentVariable("ML_ACCESS_TOKEN") ?? "";
    }

    [Fact]
    public async Task GetMlOrders_WithRealToken_ReturnsOrdersList()
    {
        Skip.If(string.IsNullOrEmpty(_mlToken), "ML_ACCESS_TOKEN não configurado");

        var response = await _client.GetAsync("/api/ml/orders");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent,
            $"Esperado 200/204, recebido {response.StatusCode}");
    }

    [Fact]
    public async Task GetMlOrderById_WithRealToken_ReturnsOrderOrNotFound()
    {
        Skip.If(string.IsNullOrEmpty(_mlToken), "ML_ACCESS_TOKEN não configurado");

        // Busca lista primeiro para pegar um ID real
        var listResp = await _client.GetAsync("/api/ml/orders");
        if (listResp.StatusCode == HttpStatusCode.NoContent) return;

        var body = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        var orders = body.GetProperty("orders");
        if (orders.GetArrayLength() == 0) return;

        var orderId = orders[0].GetProperty("id").GetString();
        var resp = await _client.GetAsync($"/api/ml/orders/{orderId}");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task MlWebhook_Post_ReturnsAccepted()
    {
        var payload = new
        {
            resource = "/orders/ORDER-ML-TEST",
            user_id = 123456789,
            topic = "orders",
            application_id = 2786639248653015L,
            attempts = 1,
            sent = DateTime.UtcNow.ToString("o"),
            received = DateTime.UtcNow.ToString("o")
        };

        var response = await _client.PostAsJsonAsync("/api/ml/webhook", payload);

        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Accepted,
            $"Webhook esperado 200/202, recebido {response.StatusCode}");
    }

    [Fact]
    public async Task MlOAuthCallback_WithoutCode_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/ml/callback");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
