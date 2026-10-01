using System.Text.Json;
using StackExchange.Redis;
using UcpAgent.Domain.Fulfillment;

namespace UcpAgent.Infrastructure.Fulfillment;

/// <summary>
/// Implementação Redis do IFulfillmentRepository.
/// Persiste o estado atual do agregado e o histórico de eventos.
///
/// Estrutura no Redis:
///   fulfillment:{orderId}          → JSON do estado atual (FulfillmentAggregate)
///   fulfillment:{orderId}:history  → Lista JSON de FulfillmentEvent (append-only)
///
/// TODO migração para PostgreSQL:
///   - Tabela fulfillments  (state)
///   - Tabela fulfillment_events (append-only, Event Sourcing)
///   - Index em order_id + status para ListByStatusAsync
/// </summary>
public sealed class RedisFulfillmentRepository(IConnectionMultiplexer redis) : IFulfillmentRepository
{
    private static readonly TimeSpan Ttl     = TimeSpan.FromDays(90);
    private static readonly JsonSerializerOptions Json =
        new() { PropertyNameCaseInsensitive = true };

    private IDatabase Db => redis.GetDatabase();

    private static string StateKey(string orderId)   => $"fulfillment:{orderId}";
    private static string HistoryKey(string orderId) => $"fulfillment:{orderId}:history";

    // ── State ─────────────────────────────────────────────────────────────────

    public async Task<FulfillmentAggregate?> GetByOrderIdAsync(string orderId, CancellationToken ct = default)
    {
        var json = await Db.StringGetAsync(StateKey(orderId));
        if (json.IsNullOrEmpty) return null;

        // Reconstitui a partir do histórico (garante consistência)
        var history = await GetHistoryAsync(orderId, ct);
        return history.Count > 0
            ? FulfillmentAggregate.Reconstitute(orderId, history)
            : null;
    }

    public async Task SaveAsync(FulfillmentAggregate aggregate, CancellationToken ct = default)
    {
        // Persiste estado atual
        var stateDto = new FulfillmentStateDto(
            aggregate.OrderId,
            aggregate.CurrentStatus.ToString(),
            aggregate.TrackingCode,
            aggregate.CarrierCode,
            aggregate.CreatedAt,
            aggregate.UpdatedAt);

        await Db.StringSetAsync(StateKey(aggregate.OrderId),
            JsonSerializer.Serialize(stateDto), Ttl);

        // Persiste apenas os novos eventos (append-only)
        var existingCount = await Db.ListLengthAsync(HistoryKey(aggregate.OrderId));
        var newEvents = aggregate.Events.Skip((int)existingCount).ToList();

        if (newEvents.Count > 0)
        {
            var entries = newEvents
                .Select(e => (RedisValue)JsonSerializer.Serialize(e))
                .ToArray();
            await Db.ListRightPushAsync(HistoryKey(aggregate.OrderId), entries);
            await Db.KeyExpireAsync(HistoryKey(aggregate.OrderId), Ttl);
        }
    }

    // ── History ───────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<FulfillmentEvent>> GetHistoryAsync(
        string orderId, CancellationToken ct = default)
    {
        var entries = await Db.ListRangeAsync(HistoryKey(orderId));
        return entries
            .Where(e => !e.IsNullOrEmpty)
            .Select(e => JsonSerializer.Deserialize<FulfillmentEvent>((string)e!, Json)!)
            .ToList()
            .AsReadOnly();
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<FulfillmentAggregate>> ListByStatusAsync(
        FulfillmentStatus status, CancellationToken ct = default)
    {
        // TODO: Redis não tem query nativa — usar SCAN com padrão + filtro
        // Em produção, migrar para PostgreSQL com index em status
        var server  = redis.GetServer(redis.GetEndPoints().First());
        var keys    = server.Keys(pattern: "fulfillment:*", pageSize: 200)
                            .Where(k => !k.ToString().Contains(":history"))
                            .ToList();

        var result = new List<FulfillmentAggregate>();
        foreach (var key in keys)
        {
            var orderId  = key.ToString().Replace("fulfillment:", "");
            var history  = await GetHistoryAsync(orderId, ct);
            if (history.Count == 0) continue;
            var agg = FulfillmentAggregate.Reconstitute(orderId, history);
            if (agg.CurrentStatus == status) result.Add(agg);
        }
        return result.AsReadOnly();
    }
}

// ── DTO interno para serialização do estado ───────────────────────────────────
internal record FulfillmentStateDto(
    string   OrderId,
    string   Status,
    string?  TrackingCode,
    string?  CarrierCode,
    DateTime CreatedAt,
    DateTime UpdatedAt);
