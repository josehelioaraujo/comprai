using System.Net.Http.Json;
using System.Text.Json;
using UcpAgent.SharedKernel.Attributes;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Catalog.OpenFoodFacts;

[CatalogPlugin("OpenFoodFacts")]
public sealed class OpenFoodFactsPlugin : IProductCatalogPort
{
    private const string BaseUrl = "https://world.openfoodfacts.org/cgi/search.pl";

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;

    public OpenFoodFactsPlugin(HttpClient http) => _http = http;

    public string SourceName => "OpenFoodFacts";

    public async Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}"
                + $"?search_terms={Uri.EscapeDataString(request.Query)}"
                + $"&search_simple=1&action=process&json=1"
                + $"&page={request.Page}&page_size={request.PageSize}";

        OffSearchResponse? response;
        try
        {
            response = await _http.GetFromJsonAsync<OffSearchResponse>(url, JsonOpts, cancellationToken);
        }
        catch { return Empty(request); }

        if (response is null)
            return Empty(request);

        var items = response.Products
            .Where(p => !string.IsNullOrWhiteSpace(p.Product_Name))
            .Select(p => new ProductDto(
                Id:                p.Id,
                Title:             p.Product_Name,
                Price:             0,           // OFF não fornece preço
                ImageUrl:          p.Image_Url ?? string.Empty,
                Url:               p.Url ?? $"https://world.openfoodfacts.org/product/{p.Id}",
                Category:          p.Categories?.Split(',').FirstOrDefault()?.Trim() ?? "Alimentos",
                Source:            SourceName,
                OriginalPrice:     null,
                AvailableQuantity: 0))
            .ToList();

        return new SearchResult(items, response.Count, request.Page, request.PageSize, SourceName);
    }

    private SearchResult Empty(SearchRequest r) =>
        new([], 0, r.Page, r.PageSize, SourceName);
}
