using Dapper;
using UcpAgent.Infrastructure.Persistence;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

public sealed record OrderHistorySummary(
    string   OrderId,
    string   FinalStatus,
    decimal  Total,
    string?  TrackingCode,
    string?  PaymentMethod,
    string?  ShippingMethod,
    DateTime OccurredAt);

public sealed record FulfillmentEventSummary(
    string   Status,
    string   Description,
    DateTime OccurredAt,
    string?  TrackingCode,
    string?  Location);

public sealed class OrdersQueryRepository(IDbConnectionFactory factory)
{
    // F4 — GET /api/orders?sessionId → lê order_history
    public Task<IReadOnlyList<OrderHistorySummary>> GetHistoryBySessionAsync(
        string sessionId, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            var rows = await conn.QueryAsync<OrderHistorySummary>("""
                SELECT oh.order_id     AS OrderId,
                       oh.final_status AS FinalStatus,
                       o.total_amount  AS Total,
                       oh.tracking_code AS TrackingCode,
                       p.method        AS PaymentMethod,
                       o.shipping_method AS ShippingMethod,
                       oh.occurred_at  AS OccurredAt
                  FROM order_history oh
                  JOIN "order" o   ON o.id = oh.order_id
                  LEFT JOIN payment p ON p.order_id = o.id
                 WHERE o.session_id = @sessionId::uuid
                 ORDER BY oh.occurred_at DESC
                """, new { sessionId });
            return (IReadOnlyList<OrderHistorySummary>)rows.ToList().AsReadOnly();
        }, ct);

    // F5 — GET /api/orders/{id}/fulfillment → lê fulfillment_event (timeline ao vivo)
    public Task<IReadOnlyList<FulfillmentEventSummary>> GetFulfillmentTimelineAsync(
        string orderId, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            var rows = await conn.QueryAsync<FulfillmentEventSummary>("""
                SELECT status       AS Status,
                       description  AS Description,
                       occurred_at  AS OccurredAt,
                       tracking_code AS TrackingCode,
                       location     AS Location
                  FROM fulfillment_event
                 WHERE order_id = @orderId
                 ORDER BY occurred_at ASC
                """, new { orderId });
            return (IReadOnlyList<FulfillmentEventSummary>)rows.ToList().AsReadOnly();
        }, ct);
}