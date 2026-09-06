using System.Text.Json;
using StackExchange.Redis;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Cart;

public sealed class RedisCartAdapter(IConnectionMultiplexer redis) : ICartPort
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(72);
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private IDatabase Db => redis.GetDatabase();

    private static string Key(string sessionId) => $"cart:{sessionId}";

    public async Task<string> AddItemAsync(string sessionId, ProductDto product, int quantity, CancellationToken ct = default)
    {
        var key   = Key(sessionId);
        var items = await LoadAsync(key);

        var existing = items.FirstOrDefault(i => i.Product.Id == product.Id && i.Product.Source == product.Source);
        if (existing is not null)
        {
            items.Remove(existing);
            var newQty    = existing.Quantity + quantity;
            var newSubtotal = product.Price * newQty;
            items.Add(existing with { Quantity = newQty, Subtotal = newSubtotal });
        }
        else
        {
            var itemId = Guid.NewGuid().ToString("N")[..8];
            items.Add(new CartItemDto(itemId, product, quantity, product.Price * quantity));
        }

        await SaveAsync(key, items);
        return existing?.ItemId ?? items.Last().ItemId;
    }

    public async Task<IReadOnlyList<CartItemDto>> GetItemsAsync(string sessionId, CancellationToken ct = default)
        => await LoadAsync(Key(sessionId));

    public async Task RemoveItemAsync(string sessionId, string itemId, CancellationToken ct = default)
    {
        var key   = Key(sessionId);
        var items = await LoadAsync(key);
        items.RemoveAll(i => i.ItemId == itemId);
        await SaveAsync(key, items);
    }

    public async Task ClearAsync(string sessionId, CancellationToken ct = default)
        => await Db.KeyDeleteAsync(Key(sessionId));

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<List<CartItemDto>> LoadAsync(string key)
    {
        var json = await Db.StringGetAsync(key);
        if (json.IsNullOrEmpty) return [];
        return JsonSerializer.Deserialize<List<CartItemDto>>((string)json!, JsonOpts) ?? [];
    }

    private async Task SaveAsync(string key, List<CartItemDto> items)
    {
        var json = JsonSerializer.Serialize(items);
        await Db.StringSetAsync(key, json, Ttl);
    }
}
