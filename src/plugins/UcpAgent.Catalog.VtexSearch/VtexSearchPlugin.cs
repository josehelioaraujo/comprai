using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using UcpAgent.SharedKernel.Attributes;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Catalog.VtexSearch;

[CatalogPlugin("VtexSearch")]
public sealed class VtexSearchPlugin : IProductCatalogPort
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly string _account;

    public VtexSearchPlugin(HttpClient http, IConfiguration config)
    {
        _http = http;
        _account = config["VtexSearch:AccountName"] ?? "vtex";
    }

    public string SourceName => "VtexSearch";

    public async Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var url = $"https://{_account}.myvtex.com/_v/api/intelligent-search/product_search"
                + $"?query={Uri.EscapeDataString(request.Query)}"
                + $"&page={request.Page}&count={request.PageSize}";

        if (!string.IsNullOrWhiteSpace(request.Category))
            url += $"&category={Uri.EscapeDataString(request.Category)}";

        VtexSearchResponse? response;
        try
        {
            response = await _http.GetFromJsonAsync<VtexSearchResponse>(url, JsonOpts, cancellationToken);
        }
        catch { return Empty(request); }

        if (response is null)
            return Empty(request);

        var items = response.Products
            .Where(p => p.Items.Count > 0)
            .Select(p =>
            {
                var sku   = p.Items[0];
                var offer = sku.Sellers.FirstOrDefault()?.CommertialOffer;
                var image = sku.Images.FirstOrDefault()?.ImageUrl ?? string.Empty;
                var cat   = p.Categories.FirstOrDefault() ?? string.Empty;

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

        return new SearchResult(items, response.Pagination.Total, request.Page, request.PageSize, SourceName);
    }

    private SearchResult Empty(SearchRequest r) =>
        new([], 0, r.Page, r.PageSize, SourceName);
}
