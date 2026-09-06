using System.Text.Json;
using StackExchange.Redis;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Orders;

public sealed class RedisOrderAdapter(IConnectionMultiplexer redis) : IOrderPort
{
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(30);
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private IDatabase Db => redis.GetDatabase();

    public static string Key(string orderId) => $"order:{orderId}";

    public async Task<OrderStatusDto?> GetStatusAsync(string orderId, CancellationToken ct = default)
    {
        var json = await Db.StringGetAsync(Key(orderId));
        if (json.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<OrderStatusDto>(json!, JsonOpts);
    }

    public async Task SaveAsync(OrderStatusDto order)
    {
        var json = JsonSerializer.Serialize(order);
        await Db.StringSetAsync(Key(order.OrderId), json, Ttl);
    }
}
