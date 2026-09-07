using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace UcpAgent.Integration.Tests;

[Collection("IntegrationTests")]
public class ShopifyIntegrationTest : IClassFixture<CompraApiFactory>
{
    private readonly HttpClient _client;
    private readonly bool _tokenDisponivel;

    private static readonly JsonSerializerOptions _json =
        new() { PropertyNameCaseInsensitive = true };

    public ShopifyIntegrationTest(CompraApiFactory factory)
    {
        _client          = factory.CreateClient();
        _tokenDisponivel = !string.IsNullOrEmpty(
            Environment.GetEnvironmentVariable("SHOPIFY_ACCESS_TOKEN"));
    }

    [Fact]
    public async Task SearchShopify_SemToken_NaoQuebraApi()
    {
        var response = await _client.GetAsync("/api/search?q=teste&page=1&pageSize=10");
        Assert.True(
            response.StatusCode != HttpStatusCode.InternalServerError,
            $"API não deve retornar 500. Status: {response.StatusCode}");
    }

    [SkippableFact]
    public async Task SearchShopify_ComToken_RetornaProdutos()
    {
        Skip.If(!_tokenDisponivel, "SHOPIFY_ACCESS_TOKEN ausente — teste pulado");

        var response = await _client.GetAsync("/api/search?q=snowboard&page=1&pageSize=10");
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NoContent,
            $"Esperado 200/204, recebido {response.StatusCode}");
    }

    [SkippableFact]
    public async Task SearchShopify_ComToken_RetornaCamposObrigatorios()
    {
        Skip.If(!_tokenDisponivel, "SHOPIFY_ACCESS_TOKEN ausente — teste pulado");

        var response = await _client.GetAsync("/api/search?q=snowboard&page=1&pageSize=10");
        response.EnsureSuccessStatusCode();

        var body   = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SearchResult>(body, _json);

        Assert.NotNull(result);

        var shopifyItems = result!.Items
            .Where(p => string.Equals(p.Source, "shopify", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(shopifyItems.Count > 0,
            "Plugin GraphQL deve retornar ao menos 1 item Shopify para 'snowboard'");

        foreach (var item in shopifyItems)
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Id),    "Id obrigatório");
            Assert.False(string.IsNullOrWhiteSpace(item.Title), "Title obrigatório");
            Assert.True(item.Price >= 0,                        "Price >= 0");
        }
    }

    [SkippableFact]
    public async Task SearchShopify_ComToken_RetornaSourceShopify()
    {
        Skip.If(!_tokenDisponivel, "SHOPIFY_ACCESS_TOKEN ausente — teste pulado");

        var response = await _client.GetAsync("/api/search?q=snowboard&page=1&pageSize=10");
        response.EnsureSuccessStatusCode();

        var body   = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SearchResult>(body, _json);

        Assert.NotNull(result);

        var temShopify = result!.Items
            .Any(p => string.Equals(p.Source, "shopify", StringComparison.OrdinalIgnoreCase));

        Assert.True(temShopify,
            "Ao menos 1 item deve ter source='shopify' após migração GraphQL");
    }

    // ── Records auxiliares ────────────────────────────────────────────────────

    private sealed record SearchResult(List<ProductDto> Items, int TotalItems);
    private sealed record ProductDto(string Id, string Source, string Title, decimal Price);
}
