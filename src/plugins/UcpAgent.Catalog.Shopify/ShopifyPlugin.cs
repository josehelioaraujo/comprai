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
                description
                vendor
                productType
                handle
                tags
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
            _logger.LogWarning("[Shopify] AccessToken nao configurado - ignorando.");
            return new SearchResult([], 0, request.Page, request.PageSize, SourceName);
        }

        try
        {
            var cursor = await ResolveCursorAsync(request.Query, request.Page, request.PageSize, cancellationToken);
            var products = await FetchPageAsync(request.Query, request.PageSize, cursor, cancellationToken);

            var filtered = products;
            if (request.MinPrice.HasValue)
                filtered = filtered.Where(p => p.Price >= request.MinPrice.Value).ToList();
            if (request.MaxPrice.HasValue)
                filtered = filtered.Where(p => p.Price <= request.MaxPrice.Value).ToList();

            return new SearchResult(filtered, filtered.Count, request.Page, request.PageSize, SourceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Shopify] Erro ao buscar produtos via GraphQL.");
            return new SearchResult([], 0, request.Page, request.PageSize, SourceName);
        }
    }

    private async Task<string?> ResolveCursorAsync(
        string query, int page, int pageSize, CancellationToken ct)
    {
        if (page <= 1) return null;

        const string CursorQuery = """
            query GetCursor($query: String!, $first: Int!, $after: String) {
              products(query: $query, first: $first, after: $after) {
                pageInfo { hasNextPage endCursor }
              }
            }
            """;

        string? cursor = null;
        for (int i = 1; i < page; i++)
        {
            var payload = BuildPayload(CursorQuery, query, pageSize, cursor);
            var response = await PostGraphQlAsync(payload, ct);
            cursor = response?.Data?.Products?.PageInfo?.EndCursor;
            if (cursor is null) break;
        }

        return cursor;
    }

    private async Task<List<ProductDto>> FetchPageAsync(
        string query, int pageSize, string? cursor, CancellationToken ct)
    {
        var payload = BuildPayload(ProductsQuery, query, pageSize, cursor);
        var response = await PostGraphQlAsync(payload, ct);
        if (response is null) return [];

        return response.Data.Products.Edges
            .Select(e => MapToProduct(e.Node))
            .ToList();
    }

    private async Task<ShopifyGraphQlResponse?> PostGraphQlAsync(object payload, CancellationToken ct)
    {
        var url = $"https://{_options.StoreUrl}/admin/api/2026-07/graphql.json";
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpResponse = await _http.PostAsync(url, content, ct);
        httpResponse.EnsureSuccessStatusCode();

        return await httpResponse.Content
            .ReadFromJsonAsync<ShopifyGraphQlResponse>(_jsonOptions, ct);
    }

    private static object BuildPayload(string gqlQuery, string search, int first, string? after) => new
    {
        query = gqlQuery,
        variables = new { query = search, first, after },
    };

    private static ProductDto MapToProduct(ProductNode node)
    {
        var firstVariant = node.Variants.Edges.FirstOrDefault()?.Node;
        var priceStr = firstVariant?.Price.Amount ?? node.PriceRangeV2.MinVariantPrice.Amount;

        decimal.TryParse(priceStr,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var price);

        var id = node.Id.Split('/').LastOrDefault() ?? node.Id;

        return new ProductDto(
            Id:                $"shopify-{id}",
            Title:             node.Title,
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
    public string StoreUrl { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}

// ── GraphQL Models ────────────────────────────────────────────────────────────

internal sealed record ShopifyGraphQlResponse(ShopifyGraphQlData Data);
internal sealed record ShopifyGraphQlData(ProductConnection Products);
internal sealed record ProductConnection(List<ProductEdge> Edges, PageInfo PageInfo);
internal sealed record ProductEdge(ProductNode Node);
internal sealed record PageInfo(bool HasNextPage, string? EndCursor);
internal sealed record ProductNode(
    string Id, string Title, string? Description, string? Vendor,
    string? ProductType, string? Handle, List<string> Tags,
    FeaturedImage? FeaturedImage, PriceRangeV2 PriceRangeV2, VariantConnection Variants);
internal sealed record FeaturedImage(string? Url);
internal sealed record PriceRangeV2(MoneyV2 MinVariantPrice);
internal sealed record MoneyV2(string Amount, string CurrencyCode);
internal sealed record VariantConnection(List<VariantEdge> Edges);
internal sealed record VariantEdge(VariantNode Node);
internal sealed record VariantNode(string? Sku, MoneyV2 Price);
