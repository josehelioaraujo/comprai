using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UcpAgent.Catalog.MercadoLivreOrders;

public sealed class MlOrdersService(
    IHttpClientFactory httpFactory,
    MlTokenService tokenService)
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    // Lista pedidos do vendedor
    public async Task<List<MlOrderDto>> GetOrdersAsync(
        string? status = null,
        int limit = 20,
        int offset = 0,
        CancellationToken ct = default)
    {
        var token  = await tokenService.GetAccessTokenAsync(ct);
        var client = httpFactory.CreateClient("MlApi");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Pega user_id do proprio token
        var meResponse = await client.GetFromJsonAsync<MlMeResponse>("https://api.mercadolibre.com/users/me", JsonOpts, ct);
        if (meResponse is null) return [];

        var url = $"https://api.mercadolibre.com/orders/search?seller={meResponse.Id}&limit={limit}&offset={offset}";
        if (!string.IsNullOrEmpty(status))
            url += $"&order.status={status}";

        var result = await client.GetFromJsonAsync<MlOrdersResponse>(url, JsonOpts, ct);
        return result?.Results ?? [];
    }

    // Busca pedido especifico
    public async Task<MlOrderDto?> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        var token  = await tokenService.GetAccessTokenAsync(ct);
        var client = httpFactory.CreateClient("MlApi");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return await client.GetFromJsonAsync<MlOrderDto>(
            $"https://api.mercadolibre.com/orders/{orderId}", JsonOpts, ct);
    }
}

//  DTOs 
public record MlMeResponse(
    [property: JsonPropertyName("id")]       long   Id,
    [property: JsonPropertyName("nickname")] string Nickname
);

public record MlOrdersResponse(
    [property: JsonPropertyName("results")] List<MlOrderDto> Results,
    [property: JsonPropertyName("paging")]  MlPaging         Paging
);

public record MlPaging(
    [property: JsonPropertyName("total")]  int Total,
    [property: JsonPropertyName("limit")]  int Limit,
    [property: JsonPropertyName("offset")] int Offset
);

public record MlOrderDto(
    [property: JsonPropertyName("id")]           long             Id,
    [property: JsonPropertyName("status")]       string           Status,
    [property: JsonPropertyName("total_amount")] decimal          TotalAmount,
    [property: JsonPropertyName("date_created")] string           DateCreated,
    [property: JsonPropertyName("order_items")]  List<MlOrderItem> OrderItems,
    [property: JsonPropertyName("buyer")]        MlBuyer?         Buyer
);

public record MlOrderItem(
    [property: JsonPropertyName("item")]     MlItem  Item,
    [property: JsonPropertyName("quantity")] int     Quantity,
    [property: JsonPropertyName("unit_price")] decimal UnitPrice
);

public record MlItem(
    [property: JsonPropertyName("id")]    string Id,
    [property: JsonPropertyName("title")] string Title
);

public record MlBuyer(
    [property: JsonPropertyName("id")]       long   Id,
    [property: JsonPropertyName("nickname")] string Nickname
);

