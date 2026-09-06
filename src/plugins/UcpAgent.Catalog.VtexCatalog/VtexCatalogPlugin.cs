using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using UcpAgent.SharedKernel.Attributes;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Catalog.VtexCatalog;

[CatalogPlugin("VtexCatalog")]
public sealed class VtexCatalogPlugin : IProductCatalogPort
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly string _account;

    public VtexCatalogPlugin(HttpClient http, IConfiguration config)
    {
        _http = http;
        _account = config["VtexCatalog:AccountName"] ?? "vtex";
    }

    public string SourceName => "VtexCatalog";

    public async Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var from = (request.Page - 1) * request.PageSize;
        var to   = from + request.PageSize - 1;

        var term = Uri.EscapeDataString(request.Query);
        var url  = $"https://{_account}.vtexcommercestable.com.br/api/catalog_system/pub/products/search/{term}"
                 + $"?_from={from}&_to={to}";

        if (request.MinPrice.HasValue) url += $"&PriceFrom={request.MinPrice:F2}";
        if (request.MaxPrice.HasValue) url += $"&PriceTo={request.MaxPrice:F2}";

        List<VtexProduct>? products;
        try
        {
            products = await _http.GetFromJsonAsync<List<VtexProduct>>(url, JsonOpts, cancellationToken);
        }
        catch { return Empty(request); }

        if (products is null or { Count: 0 })
            return Empty(request);

        var items = products
            .Where(p => p.Items.Count > 0)
            .Select(p =>
            {
                var item   = p.Items[0];
                var offer  = item.Sellers.FirstOrDefault()?.CommertialOffer;
                var image  = item.Images.FirstOrDefault()?.ImageUrl ?? string.Empty;
                var cat    = p.Categories.FirstOrDefault() ?? string.Empty;

                return new ProductDto(
                    Id:                p.ProductId,
                    Title:             p.ProductName,
                    Price:             offer?.Price ?? 0,
                    ImageUrl:          image,
                    Url:               p.Link,
                    Category:          cat.Trim('/').Split('/').LastOrDefault() ?? cat,
                    Source:            SourceName,
                    OriginalPrice:     offer?.ListPrice,
                    AvailableQuantity: offer?.AvailableQuantity ?? 0);
            }).ToList();

        return new SearchResult(items, items.Count, request.Page, request.PageSize, SourceName);
    }

    private SearchResult Empty(SearchRequest r) =>
        new([], 0, r.Page, r.PageSize, SourceName);
}
