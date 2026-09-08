using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UcpAgent.SharedKernel.Attributes;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Catalog.Shopify;

[CatalogPlugin("Shopify")]
public sealed class ShopifyPlugin : IProductCatalogPort
{
    private readonly HttpClient _http;
    private readonly ShopifyOptions _options;
    private readonly ILogger<ShopifyPlugin> _logger;

    private const string ApiVersion = "2024-10";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private const string ProductsQuery = """
        query SearchProducts($query: String!, $first: Int!, $after: String) {
          products(query: $query, first: $first, after: $after, sortKey: RELEVANCE) {
            edges {
              node {
                id
                title
                vendor
                productType
                handle
                featuredImage { url }
                priceRangeV2 {
                  minVariantPrice { amount currencyCode }
                }
                variants(first: 1) {
                  edges {
                    node {
                      sku
                      price { amount currencyCode }
                    }
                  }
                }
              }
            }
            pageInfo { hasNextPage endCursor }
          }
        }
        """;

    public string SourceName => "Shopify";

    public ShopifyPlugin(
        HttpClient http,
        IOptions<ShopifyOptions> options,
        ILogger<ShopifyPlugin> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.AccessToken))
            _http.DefaultRequestHeaders.Add("X-Shopify-Access-Token", _options.AccessToken);
    }

    public async Task<SearchResult> SearchAsync(
        SearchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            _logger.LogWarning("[Shopify] AccessToken nao configurado.");
            return new SearchResult([], 0, request.Page, request.PageSize, SourceName);
        }

        if (string.IsNullOrWhiteSpace(_options.StoreUrl))
        {
            _logger.LogWarning("[Shopify] StoreUrl nao configurado.");
            return new SearchResult([], 0, request.Page, request.PageSize, SourceName);
        }

        try
        {
            var products = await FetchPageAsync(request.Query, request.PageSize, null, cancellationToken);

            var filtered = products;
            if (request.MinPrice.HasValue)
                filtered = filtered.Where(p => p.Price >= request.MinPrice.Value).ToList();
            if (request.MaxPrice.HasValue)
                filtered = filtered.Where(p => p.Price <= request.MaxPrice.Value).ToList();

            _logger.LogInformation("[Shopify] {Count} produtos para '{Query}'", filtered.Count, request.Query);
            return new SearchResult(filtered, filtered.Count, request.Page, request.PageSize, SourceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Shopify] Erro ao buscar produtos.");
            return new SearchResult([], 0, request.Page, request.PageSize, SourceName);
        }
    }

    private async Task<List<ProductDto>> FetchPageAsync(
        string query, int pageSize, string? cursor, CancellationToken ct)
    {
        var payload = new
        {
            query     = ProductsQuery,
            variables = new { query, first = pageSize, after = cursor }
        };

        var url     = $"https://{_options.StoreUrl}/admin/api/{ApiVersion}/graphql.json";
        var json    = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpResponse = await _http.PostAsync(url, content, ct);
        var rawBody      = await httpResponse.Content.ReadAsStringAsync(ct);

        if (!httpResponse.IsSuccessStatusCode)
        {
            _logger.LogError("[Shopify] HTTP {Status}: {Body}", (int)httpResponse.StatusCode, rawBody[..Math.Min(500, rawBody.Length)]);
            return [];
        }

        _logger.LogDebug("[Shopify] Response: {Body}", rawBody[..Math.Min(1000, rawBody.Length)]);

        var response = JsonSerializer.Deserialize<ShopifyGraphQlResponse>(rawBody, _jsonOptions);

        if (response?.Data?.Products?.Edges is null)
        {
            _logger.LogWarning("[Shopify] Response.Data.Products.Edges nulo. Body: {Body}", rawBody[..Math.Min(500, rawBody.Length)]);
            return [];
        }

        return response.Data.Products.Edges
            .Where(e => e?.Node is not null)
            .Select(e => MapToProduct(e.Node!))
            .ToList();
    }

    private static ProductDto MapToProduct(ProductNode node)
    {
        var firstVariant = node.Variants?.Edges?.FirstOrDefault()?.Node;
        var priceStr     = firstVariant?.Price?.Amount
                           ?? node.PriceRangeV2?.MinVariantPrice?.Amount
                           ?? "0";

        decimal.TryParse(priceStr,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var price);

        var id = node.Id?.Split('/').LastOrDefault() ?? node.Id ?? "0";

        return new ProductDto(
            Id:                $"shopify-{id}",
            Title:             node.Title ?? string.Empty,
            Price:             price,
            ImageUrl:          node.FeaturedImage?.Url ?? string.Empty,
            Url:               $"https://{node.Handle}.myshopify.com/products/{node.Handle}",
            Category:          node.ProductType ?? string.Empty,
            Source:            "Shopify",
            OriginalPrice:     null,
            AvailableQuantity: null
        );
    }
}

// ── Options ───────────────────────────────────────────────────────────────────

public sealed class ShopifyOptions
{
    public string StoreUrl    { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}

// ── GraphQL Models ────────────────────────────────────────────────────────────

internal sealed record ShopifyGraphQlResponse(ShopifyGraphQlData? Data);
internal sealed record ShopifyGraphQlData(ProductConnection? Products);
internal sealed record ProductConnection(List<ProductEdge>? Edges, PageInfo? PageInfo);
internal sealed record ProductEdge(ProductNode? Node);
internal sealed record PageInfo(bool HasNextPage, string? EndCursor);
internal sealed record ProductNode(
    string? Id, string? Title, string? Vendor,
    string? ProductType, string? Handle,
    FeaturedImage? FeaturedImage, PriceRangeV2? PriceRangeV2, VariantConnection? Variants);
internal sealed record FeaturedImage(string? Url);
internal sealed record PriceRangeV2(MoneyV2? MinVariantPrice);
internal sealed record MoneyV2(string? Amount, string? CurrencyCode);
internal sealed record VariantConnection(List<VariantEdge>? Edges);
internal sealed record VariantEdge(VariantNode? Node);
internal sealed record VariantNode(string? Sku, MoneyV2? Price);
