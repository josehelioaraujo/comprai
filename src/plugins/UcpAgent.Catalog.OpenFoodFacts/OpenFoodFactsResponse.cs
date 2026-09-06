using System.Text.Json.Serialization;

namespace UcpAgent.Catalog.OpenFoodFacts;

internal sealed record OffSearchResponse(
    int Count,
    int Page,
    [property: JsonPropertyName("page_size")] int PageSize,
    List<OffProduct> Products);

internal sealed record OffProduct(
    string Id,
    [property: JsonPropertyName("product_name")] string Product_Name,
    [property: JsonPropertyName("image_url")]    string? Image_Url,
    string? Url,
    string? Categories);
