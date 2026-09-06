using System.Net.Http.Json;
using System.Text.Json;
using UcpAgent.SharedKernel.Attributes;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Catalog.MercadoLivre;

[CatalogPlugin("MercadoLivre")]
public sealed class MercadoLivrePlugin : IProductCatalogPort
{
    private const string BaseUrl = "https://api.mercadolibre.com";
    private const string SiteId  = "MLB";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public MercadoLivrePlugin(HttpClient http) => _http = http;

    public string SourceName => "MercadoLivre";

    public async Task<SearchResult> SearchAsync(
        SearchRequest request, CancellationToken cancellationToken = default)
    {
        var offset = (request.Page - 1) * request.PageSize;
        var limit  = request.PageSize;

        var url = $"{BaseUrl}/sites/{SiteId}/search"
                + $"?q={Uri.EscapeDataString(request.Query)}"
                + $"&offset={offset}&limit={limit}";

        if (!string.IsNullOrWhiteSpace(request.Category))
            url += $"&category={Uri.EscapeDataString(request.Category)}";

        if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
        {
            var min = request.MinPrice?.ToString("F0") ?? "*";
            var max = request.MaxPrice?.ToString("F0") ?? "*";
            url += $"&price={min}-{max}";
        }

        MlSearchResponse? response;

        try
        {
            response = await _http.GetFromJsonAsync<MlSearchResponse>(
                url, JsonOpts, cancellationToken);
        }
        catch
        {
            return new SearchResult([], 0, request.Page, request.PageSize, SourceName);
        }

        if (response is null)
            return new SearchResult([], 0, request.Page, request.PageSize, SourceName);

        var items = response.Results.Select(i => new ProductDto(
            Id:                i.Id,
            Title:             i.Title,
            Price:             i.Price,
            ImageUrl:          i.Thumbnail.Replace("http://", "https://"),
            Url:               i.Permalink,
            Category:          i.Category_Id,
            Source:            SourceName,
            OriginalPrice:     i.Original_Price,
            AvailableQuantity: i.Available_Quantity
        )).ToList();

        return new SearchResult(items, response.Paging.Total, request.Page, request.PageSize, SourceName);
    }
}
