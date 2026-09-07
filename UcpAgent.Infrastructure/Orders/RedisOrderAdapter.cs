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
    public static string SessionKey(string sessionId) => $"order:session:{sessionId}";

    public async Task<OrderStatusDto?> GetStatusAsync(string orderId, CancellationToken ct = default)
    {
        var json = await Db.StringGetAsync(Key(orderId));
        if (json.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<OrderStatusDto>((string)json!, JsonOpts);
    }

    public async Task SaveAsync(OrderStatusDto order)
    {
        var json = JsonSerializer.Serialize(order);
        await Db.StringSetAsync(Key(order.OrderId), json, Ttl);
    }

    public async Task<string?> GetOrderIdBySessionAsync(string sessionId, CancellationToken ct = default)
    {
        var val = await Db.StringGetAsync(SessionKey(sessionId));
        return val.IsNullOrEmpty ? null : (string)val!;
    }
}
