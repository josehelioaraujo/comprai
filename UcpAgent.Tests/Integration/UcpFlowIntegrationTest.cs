using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace UcpAgent.Tests.Integration;

[Collection("IntegrationTests")]
public class UcpFlowIntegrationTest : IClassFixture<CompraApiFactory>
{
    private readonly HttpClient _client;
    private static string? _cartId;
    private static string? _orderId;
    private static string? _productId;

    public UcpFlowIntegrationTest(CompraApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─── STEP 1: Search ──────────────────────────────────────────────────────

    [Fact, TestPriority(1)]
    public async Task Search_MercadoLivre_ReturnsProducts()
    {
        var response = await _client.GetAsync("/api/products/search?q=notebook&source=MercadoLivre&limit=3");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var products = body.GetProperty("products");

        Assert.True(products.GetArrayLength() > 0, "Deve retornar ao menos 1 produto");

        // Guarda o primeiro productId para os próximos steps
        _productId = products[0].GetProperty("id").GetString();
        Assert.False(string.IsNullOrEmpty(_productId));
    }

    // ─── STEP 2: Add to Cart ─────────────────────────────────────────────────

    [Fact, TestPriority(2)]
    public async Task AddToCart_WithMlProduct_ReturnsCartId()
    {
        var pid = _productId ?? "MLB123456"; // fallback para não depender de ordem

        var payload = new
        {
            productId = pid,
            quantity = 1,
            source = "MercadoLivre"
        };

        var response = await _client.PostAsJsonAsync("/api/cart", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        _cartId = body.GetProperty("cartId").GetString();

        Assert.False(string.IsNullOrEmpty(_cartId));
    }

    // ─── STEP 3: Get Cart ────────────────────────────────────────────────────

    [Fact, TestPriority(3)]
    public async Task GetCart_ReturnsItemsAdded()
    {
        var cid = _cartId ?? "test-cart-001";

        var response = await _client.GetAsync($"/api/cart/{cid}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = body.GetProperty("items");

        Assert.True(items.GetArrayLength() > 0, "Carrinho deve ter itens");
    }

    // ─── STEP 4: Checkout ────────────────────────────────────────────────────

    [Fact, TestPriority(4)]
    public async Task Checkout_WithValidCart_ReturnsOrderId()
    {
        var cid = _cartId ?? "test-cart-001";

        var payload = new { cartId = cid };
        var response = await _client.PostAsJsonAsync("/api/checkout", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        _orderId = body.GetProperty("orderId").GetString();

        Assert.False(string.IsNullOrEmpty(_orderId));
        Assert.StartsWith("ORDER-", _orderId);
    }

    // ─── STEP 5: Get Order ───────────────────────────────────────────────────

    [Fact, TestPriority(5)]
    public async Task GetOrder_ReturnsCreatedOrder()
    {
        var oid = _orderId ?? "ORDER-TEST001";

        var response = await _client.GetAsync($"/api/orders/{oid}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var status = body.GetProperty("status").GetString();

        Assert.False(string.IsNullOrEmpty(status));
    }
}
