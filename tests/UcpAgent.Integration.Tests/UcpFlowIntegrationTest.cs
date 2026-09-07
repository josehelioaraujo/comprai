using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace UcpAgent.Integration.Tests;

[Collection("IntegrationTests")]
[TestCaseOrderer("UcpAgent.Integration.Tests.PriorityOrderer", "UcpAgent.Integration.Tests")]
public class UcpFlowIntegrationTest : IClassFixture<CompraApiFactory>
{
    private readonly HttpClient _client;
    private static string _sessionId = Guid.NewGuid().ToString("N")[..8];
    private static string? _orderId;

    public UcpFlowIntegrationTest(CompraApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact, TestPriority(1)]
    public async Task Step1_Search_ReturnsProducts()
    {
        // page e pageSize obrigatórios
        var response = await _client.GetAsync("/api/search?q=notebook&page=1&pageSize=3");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact, TestPriority(2)]
    public async Task Step2_AddToCart_ReturnsOk()
    {
        // Payload real: { product: ProductDto, quantity }
        var payload = new
        {
            product = new
            {
                id       = "MLB123456",
                name     = "Notebook Teste",
                price    = 1999.99m,
                source   = "MercadoLivre",
                imageUrl = "",
                url      = ""
            },
            quantity = 1
        };

        var response = await _client.PostAsJsonAsync($"/api/cart/{_sessionId}/items", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact, TestPriority(3)]
    public async Task Step3_GetCart_ReturnsOk()
    {
        var response = await _client.GetAsync($"/api/cart/{_sessionId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact, TestPriority(4)]
    public async Task Step4_Checkout_ReturnsOrderId()
    {
        // CustomerDto obrigatório
        var payload = new
        {
            name    = "Teste Usuario",
            email   = "teste@comprai.com",
            address = "Rua Teste, 123"
        };

        var response = await _client.PostAsJsonAsync($"/api/checkout/{_sessionId}", payload);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        _orderId = body.TryGetProperty("orderId", out var oid) ? oid.GetString() :
                   body.TryGetProperty("id",      out var id)  ? id.GetString()  : null;
        Assert.False(string.IsNullOrEmpty(_orderId));
    }

    [Fact, TestPriority(5)]
    public async Task Step5_GetOrder_ReturnsOk()
    {
        var oid = _orderId ?? "ORDER-TEST001";
        var response = await _client.GetAsync($"/api/orders/{oid}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
