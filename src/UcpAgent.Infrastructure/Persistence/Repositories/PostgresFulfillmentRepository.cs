using Dapper;
using UcpAgent.Domain.Fulfillment;
using UcpAgent.Infrastructure.Persistence;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

public sealed class PostgresFulfillmentRepository : IFulfillmentRepository
{
    private readonly IDbConnectionFactory _factory;

    public PostgresFulfillmentRepository(IDbConnectionFactory factory)
        => _factory = factory;

    public async Task<FulfillmentAggregate?> GetByOrderIdAsync(string orderId,
        CancellationToken ct = default)
    {
        var history = await GetHistoryAsync(orderId, ct);
        if (!history.Any()) return null;
        return FulfillmentAggregate.Reconstitute(orderId, history);
    }

    public async Task SaveAsync(FulfillmentAggregate aggregate, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateAsync(ct);

        var existingCount = await conn.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM fulfillment_event WHERE order_id = @orderId::uuid",
            new { orderId = aggregate.OrderId });

        var newEvents = aggregate.Events.Skip(existingCount).ToList();
        if (!newEvents.Any()) return;

        foreach (var evt in newEvents)
        {
            await conn.ExecuteAsync("""
                INSERT INTO fulfillment_event (
                    order_id, status, location,
                    tracking_code, description, occurred_at
                ) VALUES (
                    @orderId::uuid, @status, @location,
                    @trackingCode, @description, @occurredAt
                )
                """,
                new
                {
                    orderId      = evt.OrderId,
                    status       = evt.Status.ToString().ToLower(),
                    location     = evt.Location,
                    trackingCode = evt.TrackingCode,
                    description  = evt.Description,
                    occurredAt   = evt.OccurredAt
                });
        }
    }

    public async Task<IReadOnlyList<FulfillmentEvent>> GetHistoryAsync(string orderId,
        CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateAsync(ct);
        var rows = await conn.QueryAsync<FulfillmentEventRow>("""
            SELECT order_id, status, description,
                   occurred_at, tracking_code, location
              FROM fulfillment_event
             WHERE order_id = @orderId::uuid
             ORDER BY occurred_at ASC
            """,
            new { orderId });

        return rows.Select(r => new FulfillmentEvent(
            OrderId:      r.OrderId,
            Status:       Enum.Parse<FulfillmentStatus>(r.Status, ignoreCase: true),
            Description:  r.Description,
            OccurredAt:   r.OccurredAt,
            TrackingCode: r.TrackingCode,
            Location:     r.Location
        )).ToList();
    }

    public async Task<IReadOnlyList<FulfillmentAggregate>> ListByStatusAsync(
        FulfillmentStatus status, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateAsync(ct);

        var orderIds = await conn.QueryAsync<string>("""
            SELECT DISTINCT order_id::text
              FROM fulfillment_event
             WHERE status = @status
               AND order_id NOT IN (
                   SELECT order_id FROM fulfillment_event
                    WHERE status IN ('delivered','cancelled','returned')
               )
            """,
            new { status = status.ToString().ToLower() });

        var result = new List<FulfillmentAggregate>();
        foreach (var id in orderIds)
        {
            var agg = await GetByOrderIdAsync(id, ct);
            if (agg is not null) result.Add(agg);
        }
        return result;
    }

    private sealed record FulfillmentEventRow(
        string   OrderId,
        string   Status,
        string   Description,
        DateTime OccurredAt,
        string?  TrackingCode,
        string?  Location);
}
