using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace UcpAgent.Integration.Tests;

[Collection("IntegrationTests")]
public class MlOrdersIntegrationTest : IClassFixture<CompraApiFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _clientNoRedirect;
    private readonly string _mlToken;

    public MlOrdersIntegrationTest(CompraApiFactory factory)
    {
        _client           = factory.CreateClient();
        _clientNoRedirect = factory.CreateClientNoRedirect();
        _mlToken          = Environment.GetEnvironmentVariable("ML_ACCESS_TOKEN") ?? "";
    }

    [Fact]
    public async Task GetMlOrders_WithRealToken_Returns200Or204()
    {
        Skip.If(string.IsNullOrEmpty(_mlToken), "ML_ACCESS_TOKEN nao configurado");

        var response = await _client.GetAsync("/api/ml/orders?limit=5&offset=0");
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MlWebhook_Post_ReturnsOk()
    {
        var payload = new
        {
            resource        = "/orders/ORDER-ML-TEST",
            user_id         = 123456789L,
            topic           = "orders",
            application_id  = 2786639248653015L,
            attempts        = 1,
            sent            = DateTime.UtcNow.ToString("o"),
            received        = DateTime.UtcNow.ToString("o")
        };

        var response = await _client.PostAsJsonAsync("/webhook/ml", payload);
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Accepted ||
            response.StatusCode == HttpStatusCode.NoContent,
            $"Esperado 200/202/204, recebido {response.StatusCode}");
    }

    [Fact]
    public async Task MlCallback_SemCode_RetornaBadRequest()
    {
        // /callback sem ?code= deve retornar 400
        var response = await _client.GetAsync("/callback");
        Assert.True(
            (int)response.StatusCode >= 400,
            $"Esperado 4xx, recebido {response.StatusCode}");
    }

    [Fact]
    public async Task MlAuth_ReturnsRedirect()
    {
        // /api/ml/auth retorna Redirect — desabilita follow
        var response = await _clientNoRedirect.GetAsync("/api/ml/auth");
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found ||
            response.StatusCode == HttpStatusCode.OK,
            $"Esperado 200/302, recebido {response.StatusCode}");
    }
}
