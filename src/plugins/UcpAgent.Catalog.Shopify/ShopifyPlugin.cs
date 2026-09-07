using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using UcpAgent.SharedKernel.Attributes;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Catalog.Shopify;

[CatalogPlugin("Shopify")]
public sealed class ShopifyPlugin : IProductCatalogPort
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public ShopifyPlugin(HttpClient http) => _http = http;

    public string SourceName => "Shopify";

    public async Task<SearchResult> SearchAsync(
        SearchRequest request, CancellationToken cancellationToken = default)
    {
        var limit  = request.PageSize;
        var url = $"products.json?limit={limit}&title={Uri.EscapeDataString(request.Query)}";

        if (!string.IsNullOrWhiteSpace(request.Category))
            url += $"&product_type={Uri.EscapeDataString(request.Category)}";

        ShopifyProductsResponse? response;
        try
        {
            response = await _http.GetFromJsonAsync<ShopifyProductsResponse>(
                url, JsonOpts, cancellationToken);
        }
        catch
        {
            return new SearchResult([], 0, request.Page, request.PageSize, SourceName);
        }

        if (response?.Products is null || response.Products.Count == 0)
            return new SearchResult([], 0, request.Page, request.PageSize, SourceName);

        var items = new List<ProductDto>();
        foreach (var product in response.Products)
        {
            var variant = product.Variants.FirstOrDefault();
            if (variant is null) continue;

            var price    = decimal.TryParse(variant.Price,    System.Globalization.NumberStyles.Any,
                           System.Globalization.CultureInfo.InvariantCulture, out var p) ? p : 0m;
            var compPrice = decimal.TryParse(variant.CompareAtPrice, System.Globalization.NumberStyles.Any,
                           System.Globalization.CultureInfo.InvariantCulture, out var cp) ? cp : (decimal?)null;

            var imageUrl  = product.Images.FirstOrDefault()?.Src;
            var available = variant.InventoryQuantity > 0;

            // filtra por preco se solicitado
            if (request.MinPrice.HasValue && price < request.MinPrice.Value) continue;
            if (request.MaxPrice.HasValue && price > request.MaxPrice.Value) continue;

            items.Add(new ProductDto(
                Id:                $"shopify-{product.Id}",
                Title:             product.Title,
                Price:             price,
                ImageUrl:          imageUrl,
                Url:               null,
                Category:          product.ProductType,
                Source:            SourceName,
                OriginalPrice:     compPrice,
                AvailableQuantity: variant.InventoryQuantity
            ));
        }

        return new SearchResult(items, items.Count, request.Page, request.PageSize, SourceName);
    }
}

// DTOs internos
internal record ShopifyProductsResponse(
    [property: JsonPropertyName("products")] List<ShopifyProduct> Products
);

internal record ShopifyProduct(
    [property: JsonPropertyName("id")]           long              Id,
    [property: JsonPropertyName("title")]        string            Title,
    [property: JsonPropertyName("product_type")] string            ProductType,
    [property: JsonPropertyName("variants")]     List<ShopifyVariant> Variants,
    [property: JsonPropertyName("images")]       List<ShopifyImage>   Images
);

internal record ShopifyVariant(
    [property: JsonPropertyName("id")]                  long    Id,
    [property: JsonPropertyName("price")]               string  Price,
    [property: JsonPropertyName("compare_at_price")]    string? CompareAtPrice,
    [property: JsonPropertyName("inventory_quantity")]  int     InventoryQuantity,
    [property: JsonPropertyName("available")]           bool    Available
);

internal record ShopifyImage(
    [property: JsonPropertyName("src")] string Src
);
