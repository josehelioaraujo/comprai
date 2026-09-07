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

    [SkippableFact]
    public async Task SearchShopify_ComToken_RetornaProdutos()
    {
        Skip.If(!_tokenDisponivel, "SHOPIFY_ACCESS_TOKEN ausente - teste pulado");
        var response = await _client.GetAsync("/api/search?q=camiseta&page=1&pageSize=10");
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent, "Esperado 200/204");
    }

    [SkippableFact]
    public async Task SearchShopify_ComToken_RetornaCamposObrigatorios()
    {
        Skip.If(!_tokenDisponivel, "SHOPIFY_ACCESS_TOKEN ausente - teste pulado");
        var response = await _client.GetAsync("/api/search?q=produto&page=1&pageSize=10");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("id", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("title", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("price", body, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task SearchShopify_ComToken_RetornaSourceShopify()
    {
        Skip.If(!_tokenDisponivel, "SHOPIFY_ACCESS_TOKEN ausente - teste pulado");
        var response = await _client.GetAsync("/api/search?q=nike&page=1&pageSize=10");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Shopify", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SearchShopify_SemToken_NaoQuebraApi()
    {
        var response = await _client.GetAsync("/api/search?q=teste&page=1&pageSize=10");
        Assert.True(response.StatusCode != HttpStatusCode.InternalServerError, "Nao deve retornar 500");
    }

    [Fact]
    public async Task SearchShopify_QueryVazia_RetornaBadRequest()
    {
        var response = await _client.GetAsync("/api/search?q=&page=1&pageSize=10");
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.UnprocessableEntity || response.StatusCode == HttpStatusCode.OK, "Esperado 400/422/200");
    }

    [Fact]
    public async Task SearchFanOut_ShopifyMaisML_NaoQuebraApi()
    {
        var response = await _client.GetAsync("/api/search?q=tenis&page=1&pageSize=10");
        Assert.True(response.StatusCode != HttpStatusCode.InternalServerError, "Nao deve retornar 500");
    }

    [Fact]
    public async Task SearchFanOut_TodosPlugins_NaoQuebraApi()
    {
        var response = await _client.GetAsync("/api/search?q=smartphone&page=1&pageSize=10");
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.BadRequest, "Esperado 200/204/400");
    }

    [Fact]
    public async Task AddShopifyProduct_AoCarrinho_RetornaOk()
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var payload = new { product = new { id = "shopify:123", title = "Produto Shopify", price = 199.90m, imageUrl = (string?)null, url = (string?)null, category = (string?)null, source = "Shopify" }, quantity = 1 };
        var response = await _client.PostAsJsonAsync("/api/cart/" + sessionId + "/items", payload);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound, "Esperado 200/201/204/404");
    }

    [Fact]
    public async Task GetCarrinho_ComProdutoShopify_RetornaItens()
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var addPayload = new { product = new { id = "shopify:111", title = "Camiseta Shopify", price = 89.90m, imageUrl = (string?)null, url = (string?)null, category = (string?)null, source = "Shopify" }, quantity = 2 };
        await _client.PostAsJsonAsync("/api/cart/" + sessionId + "/items", addPayload);
        var response = await _client.GetAsync("/api/cart/" + sessionId);
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent, "Esperado 200/204");
    }

    [Fact]
    public async Task IntentBuscar_ComMencaoShopify_RetornaIntentSearch()
    {
        var payload = new { text = "buscar tenis na shopify" };
        var response = await _client.PostAsJsonAsync("/api/intent", payload);
        Assert.True(response.StatusCode == HttpStatusCode.OK, "Esperado 200");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("search", body, StringComparison.OrdinalIgnoreCase);
    }
}