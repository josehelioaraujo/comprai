using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace UcpAgent.Integration.Tests;

[Collection("IntegrationTests")]
public class MlOrdersIntegrationTest : IClassFixture<CompraApiFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _clientNoRedirect;

    // Token válido SOMENTE quando vier do env var do CI (não do appsettings)
    // TODO: Renovação automática via refresh_token — ver README-ML-TOKEN.md
    private readonly string _mlToken;
    private readonly bool _tokenFromEnv;

    public MlOrdersIntegrationTest(CompraApiFactory factory)
    {
        _client           = factory.CreateClient();
        _clientNoRedirect = factory.CreateClientNoRedirect();
        _mlToken          = Environment.GetEnvironmentVariable("ML_ACCESS_TOKEN") ?? "";
        _tokenFromEnv     = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ML_ACCESS_TOKEN"));
    }

    [Fact]
    public async Task GetMlOrders_WithRealToken_Returns200Or204()
    {
        // Skipa se token não veio do env var (token do appsettings pode estar expirado)
        Skip.If(!_tokenFromEnv, "ML_ACCESS_TOKEN nao configurado como env var — skip para evitar 401 com token expirado");

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
        var response = await _client.GetAsync("/callback");
        Assert.True(
            (int)response.StatusCode >= 400,
            $"Esperado 4xx, recebido {response.StatusCode}");
    }

    [Fact]
    public async Task MlAuth_ReturnsRedirect()
    {
        var response = await _clientNoRedirect.GetAsync("/api/ml/auth");
        Assert.True(
            response.StatusCode == HttpStatusCode.Redirect ||
            response.StatusCode == HttpStatusCode.Found ||
            response.StatusCode == HttpStatusCode.OK,
            $"Esperado 200/302, recebido {response.StatusCode}");
    }
}
