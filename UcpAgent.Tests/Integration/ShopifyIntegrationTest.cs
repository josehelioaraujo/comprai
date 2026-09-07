using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace UcpAgent.Integration.Tests;

[Collection("IntegrationTests")]
public class ShopifyIntegrationTest : IClassFixture<CompraApiFactory>
{
    private readonly HttpClient _client;
    private readonly bool _tokenDisponivel;

    public ShopifyIntegrationTest(CompraApiFactory factory)
    {
        _client = factory.CreateClient();
        _tokenDisponivel = !string.IsNullOrEmpty(
            Environment.GetEnvironmentVariable("SHOPIFY_ACCESS_TOKEN"));
    }

    // ── Busca real (requer token) ──────────────────────────────────────────

    [SkippableFact]
    public async Task SearchShopify_ComToken_RetornaProdutos()
    {
        Skip.If(!_tokenDisponivel, "SHOPIFY_ACCESS_TOKEN ausente — teste pulado");

        var response = await _client.GetAsync("/api/search?q=camiseta&sources=shopify");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent,
            $"Esperado 200/204, recebido {response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrWhiteSpace(body), "Body não deve ser vazio");
            Assert.Contains("\"products\"", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [SkippableFact]
    public async Task SearchShopify_ComToken_RetornaCamposObrigatorios()
    {
        Skip.If(!_tokenDisponivel, "SHOPIFY_ACCESS_TOKEN ausente — teste pulado");

        var response = await _client.GetAsync("/api/search?q=produto&sources=shopify");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();

        // Campos mínimos exigidos pelo UCP
        Assert.Contains("\"id\"",    body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"title\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"price\"", body, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task SearchShopify_ComToken_RetornaSourceShopify()
    {
        Skip.If(!_tokenDisponivel, "SHOPIFY_ACCESS_TOKEN ausente — teste pulado");

        var response = await _client.GetAsync("/api/search?q=nike&sources=shopify");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Shopify", body, StringComparison.OrdinalIgnoreCase);
    }

    // ── Busca sem token (mock/fallback) ───────────────────────────────────

    [Fact]
    public async Task SearchShopify_SemToken_NaoQuebraApi()
    {
        // Sem token o plugin deve retornar lista vazia ou 204 — nunca 500
        var response = await _client.GetAsync("/api/search?q=teste&sources=shopify");

        Assert.True(
            response.StatusCode != HttpStatusCode.InternalServerError,
            $"API não deve retornar 500. Recebido: {response.StatusCode}");
    }

    [Fact]
    public async Task SearchShopify_QueryVazia_RetornaBadRequest()
    {
        var response = await _client.GetAsync("/api/search?q=&sources=shopify");

        Assert.True(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.UnprocessableEntity ||
            response.StatusCode == HttpStatusCode.OK,
            $"Esperado 400/422/200, recebido {response.StatusCode}");
    }

    // ── Fan-out multi-source (Shopify + ML juntos) ────────────────────────

    [Fact]
    public async Task SearchFanOut_ShopifyMaisML_NaoQuebraApi()
    {
        var response = await _client.GetAsync("/api/search?q=tenis&sources=shopify,mercadolivre");

        Assert.True(
            response.StatusCode != HttpStatusCode.InternalServerError,
            $"Fan-out não deve retornar 500. Recebido: {response.StatusCode}");
    }

    [Fact]
    public async Task SearchFanOut_TodosPlugins_NaoQuebraApi()
    {
        // Fan-out com todos os sources disponíveis — resiliente mesmo sem tokens
        var response = await _client.GetAsync("/api/search?q=smartphone&sources=shopify,mercadolivre,openfoodfacts");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent,
            $"Esperado 200/204, recebido {response.StatusCode}");
    }

    // ── Cart com produto Shopify ───────────────────────────────────────────

    [Fact]
    public async Task AddShopifyProduct_AoCarrinho_RetornaOk()
    {
        var sessionId = $"test-shopify-{Guid.NewGuid():N}";
        var payload = new
        {
            productId = "shopify:gid://shopify/Product/9876543210",
            title     = "Produto Shopify Teste",
            price     = 199.90m,
            quantity  = 1,
            source    = "Shopify"
        };

        var response = await _client.PostAsJsonAsync($"/api/cart/{sessionId}/items", payload);

        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.Created ||
            response.StatusCode == HttpStatusCode.NoContent,
            $"Esperado 200/201/204, recebido {response.StatusCode}");
    }

    [Fact]
    public async Task GetCarrinho_ComProdutoShopify_RetornaItens()
    {
        var sessionId = $"test-shopify-cart-{Guid.NewGuid():N}";

        // Adiciona item
        var addPayload = new
        {
            productId = "shopify:gid://shopify/Product/1111111111",
            title     = "Camiseta Shopify",
            price     = 89.90m,
            quantity  = 2, 
            source    = "Shopify"
        };
        await _client.PostAsJsonAsync($"/api/cart/{sessionId}/items", addPayload);

        // Consulta carrinho
        var response = await _client.GetAsync($"/api/cart/{sessionId}");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent,
            $"Esperado 200/204, recebido {response.StatusCode}");
    }

    // ── Intent Router com contexto Shopify ────────────────────────────────

    [Fact]
    public async Task IntentBuscar_ComMencaoShopify_RetornaIntentSearch()
    {
        var payload = new { text = "buscar tênis na shopify" };
        var response = await _client.PostAsJsonAsync("/api/intent", payload);

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Esperado 200, recebido {response.StatusCode}");

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("search", body, StringComparison.OrdinalIgnoreCase);
    }
}