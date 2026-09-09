using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using UcpAgent.SharedKernel.Attributes;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Catalog.DummyJSON;

[CatalogPlugin("DummyJSON")]
public sealed class DummyJsonPlugin : IProductCatalogPort
{
    private readonly HttpClient               _http;
    private readonly ILogger<DummyJsonPlugin> _logger;

    public string SourceName => "DummyJSON";

    public DummyJsonPlugin(HttpClient http, ILogger<DummyJsonPlugin> logger)
    {
        _http   = http;
        _logger = logger;
        _http.BaseAddress = new Uri("https://dummyjson.com");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Comprai-UCP/1.0");
    }

    public async Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var q     = Uri.EscapeDataString(request.Query);
            var limit = Math.Min(request.PageSize, 30);
            var skip  = (request.Page - 1) * limit;

            var response = await _http.GetFromJsonAsync<DummyResponse>(
                $"/products/search?q={q}&limit={limit}&skip={skip}", cancellationToken);

            if (response is null || response.Products.Count == 0)
            {
                response = await _http.GetFromJsonAsync<DummyResponse>(
                    $"/products/category/{q}?limit={limit}&skip={skip}", cancellationToken);
            }

            if (response is null || response.Products.Count == 0)
                return new SearchResult([], 0, request.Page, request.PageSize, SourceName);

            var items = response.Products.Select(MapToDto).ToList();
            _logger.LogInformation("DummyJSON: {Count} produtos para '{Query}'", items.Count, request.Query);
            return new SearchResult(items, response.Total, request.Page, request.PageSize, SourceName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DummyJSON: erro ao buscar '{Query}'", request.Query);
            return new SearchResult([], 0, request.Page, request.PageSize, SourceName);
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

internal sealed class DummyResponse
{
    [JsonPropertyName("products")] public List<DummyProduct> Products { get; set; } = [];
    [JsonPropertyName("total")]    public int Total                   { get; set; }
}

internal sealed class DummyProduct
{
    [JsonPropertyName("id")]                 public int    Id                 { get; set; }
    [JsonPropertyName("title")]              public string Title              { get; set; } = "";
    [JsonPropertyName("price")]              public double Price              { get; set; }
    [JsonPropertyName("discountPercentage")] public double DiscountPercentage { get; set; }
    [JsonPropertyName("thumbnail")]          public string Thumbnail          { get; set; } = "";
    [JsonPropertyName("category")]           public string Category           { get; set; } = "";
    [JsonPropertyName("stock")]              public int    Stock              { get; set; }
}
