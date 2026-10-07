using Dapper;
using UcpAgent.Infrastructure.Persistence;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

public sealed record OrderHistorySummary(
    string   OrderId,
    string   Status,
    decimal  Total,
    string?  TrackingCode,
    string?  PaymentMethod,
    string?  ShippingMethod,
    string?  ShippingCity,
    string?  ShippingState,
    int      ItemCount,
    DateTime CreatedAt);

public sealed record FulfillmentEventSummary(
    string   Status,
    string   Description,
    DateTime OccurredAt,
    string?  TrackingCode,
    string?  Location);

public sealed class OrdersQueryRepository(IDbConnectionFactory factory)
{
    /// <summary>
    /// GET /api/orders?sessionId — lê tabela "order" + contagem de itens + último pagamento.
    /// Retorna todos os pedidos (ativos e finalizados) para o sessionId.
    /// </summary>
    public Task<IReadOnlyList<OrderHistorySummary>> GetHistoryBySessionAsync(
        string sessionId, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            var rows = await conn.QueryAsync<OrderHistorySummary>("""
                SELECT o.id              AS OrderId,
                       o.status         AS Status,
                       o.total_amount   AS Total,
                       o.tracking_code  AS TrackingCode,
                       p.method         AS PaymentMethod,
                       o.shipping_city  AS ShippingCity,
                       o.shipping_state AS ShippingState,
                       NULL::text       AS ShippingMethod,
                       COUNT(oi.id)     AS ItemCount,
                       o.created_at     AS CreatedAt
                  FROM "order" o
                  LEFT JOIN payment p  ON p.order_id = o.id
                  LEFT JOIN order_item oi ON oi.order_id = o.id
                 WHERE o.session_id = @sessionId::uuid
                 GROUP BY o.id, p.method
                 ORDER BY o.created_at DESC
                """, new { sessionId });
            return (IReadOnlyList<OrderHistorySummary>)rows.ToList().AsReadOnly();
        }, ct);

    /// <summary>GET /api/orders/{id}/fulfillment — lê fulfillment_event (timeline ao vivo).</summary>
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
                 WHERE order_id = @orderId::uuid
                 ORDER BY occurred_at ASC
                """, new { orderId });
            return (IReadOnlyList<FulfillmentEventSummary>)rows.ToList().AsReadOnly();
        }, ct);
}
