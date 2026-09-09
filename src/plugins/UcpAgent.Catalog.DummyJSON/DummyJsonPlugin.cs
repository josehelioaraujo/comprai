using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using UcpAgent.SharedKernel.Attributes;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Catalog.DummyJSON;

[CatalogPlugin("DummyJSON", Priority = 10)]
public sealed class DummyJsonPlugin : IProductCatalogPort
{
    private readonly HttpClient                    _http;
    private readonly ILogger<DummyJsonPlugin>      _logger;
    private const string BaseUrl = "https://dummyjson.com";

    public DummyJsonPlugin(HttpClient http, ILogger<DummyJsonPlugin> logger)
    {
        _http   = http;
        _logger = logger;
        _http.BaseAddress = new Uri(BaseUrl);
        _http.DefaultRequestHeaders.Add("User-Agent", "Comprai-UCP/1.0");
    }

    public async Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken ct = default)
    {
        try
        {
            var q     = Uri.EscapeDataString(request.Query);
            var limit = Math.Min(request.PageSize, 30);
            var skip  = (request.Page - 1) * limit;

            DummyResponse? response;

            // Tenta busca por texto
            response = await _http.GetFromJsonAsync<DummyResponse>(
                $"/products/search?q={q}&limit={limit}&skip={skip}", ct);

            // Se não encontrou, tenta por categoria
            if (response is null || response.Products.Count == 0)
            {
                response = await _http.GetFromJsonAsync<DummyResponse>(
                    $"/products/category/{q}?limit={limit}&skip={skip}", ct);
            }

            if (response is null || response.Products.Count == 0)
                return SearchResult.Empty(request.Query);

            var items = response.Products.Select(MapToDto).ToList();
            _logger.LogInformation("DummyJSON: {Count} produtos para '{Query}'", items.Count, request.Query);
            return new SearchResult(request.Query, items, response.Total);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DummyJSON: erro ao buscar '{Query}'", request.Query);
            return SearchResult.Empty(request.Query);
        }
    }

    public async Task<ProductDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        try
        {
            // id formato: dummyjson-{numericId}
            var numericId = id.Replace("dummyjson-", "");
            var product   = await _http.GetFromJsonAsync<DummyProduct>($"/products/{numericId}", ct);
            return product is null ? null : MapToDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DummyJSON: erro ao buscar id={Id}", id);
            return null;
        }
    }

    private static ProductDto MapToDto(DummyProduct p) => new(
        Id:                $"dummyjson-{p.Id}",
        Title:             p.Title,
        Price:             (decimal)p.Price,
        ImageUrl:          p.Thumbnail,
        Url:               $"https://dummyjson.com/products/{p.Id}",
        Category:          p.Category,
        Source:            "DummyJSON",
        OriginalPrice:     p.DiscountPercentage > 0
                               ? Math.Round((decimal)p.Price / (1 - (decimal)p.DiscountPercentage / 100), 2)
                               : null,
        AvailableQuantity: p.Stock
    );
}

// ── Response models ────────────────────────────────────────────────────────────
file sealed class DummyResponse
{
    [JsonPropertyName("products")] public List<DummyProduct> Products { get; set; } = [];
    [JsonPropertyName("total")]    public int Total                   { get; set; }
}

file sealed class DummyProduct
{
    [JsonPropertyName("id")]                 public int    Id                 { get; set; }
    [JsonPropertyName("title")]              public string Title              { get; set; } = "";
    [JsonPropertyName("price")]              public double Price              { get; set; }
    [JsonPropertyName("discountPercentage")] public double DiscountPercentage { get; set; }
    [JsonPropertyName("thumbnail")]          public string Thumbnail          { get; set; } = "";
    [JsonPropertyName("category")]           public string Category           { get; set; } = "";
    [JsonPropertyName("brand")]              public string Brand              { get; set; } = "";
    [JsonPropertyName("stock")]              public int    Stock              { get; set; }
}
