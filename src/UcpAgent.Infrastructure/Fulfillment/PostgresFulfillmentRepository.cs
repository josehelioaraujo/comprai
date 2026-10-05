using System.Text.Json;
using Dapper;
using UcpAgent.Domain.Fulfillment;
using UcpAgent.Infrastructure.Persistence;

namespace UcpAgent.Infrastructure.Fulfillment;

/// <summary>
/// Implementação PostgreSQL do IFulfillmentRepository.
/// Etapa 6 — append-only em fulfillment_event.
/// Etapa 7 — INSERT order_history no status final (Delivered/Cancelled/Returned).
/// Resiliência via DbResiliencePolicy (retry+jitter+CB).
/// </summary>
public sealed class PostgresFulfillmentRepository(IDbConnectionFactory factory)
    : IFulfillmentRepository
{
    private static readonly FulfillmentStatus[] FinalStatuses =
        [FulfillmentStatus.Delivered, FulfillmentStatus.Cancelled, FulfillmentStatus.Returned];

    private static readonly JsonSerializerOptions Json =
        new() { PropertyNameCaseInsensitive = true };

    // ── GetByOrderIdAsync — reconstitui agregado a partir dos eventos ─────────
    public Task<FulfillmentAggregate?> GetByOrderIdAsync(
        string orderId, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            var rows = await conn.QueryAsync<FulfillmentEventRow>("""
                SELECT status, description, occurred_at,
                       carrier_code, tracking_code, location, operator_id
                  FROM fulfillment_event
                 WHERE order_id = @orderId
                 ORDER BY occurred_at ASC
                """, new { orderId });

            var events = rows.Select(r => new FulfillmentEvent(
                OrderId:     orderId,
                Status:      Enum.Parse<FulfillmentStatus>(r.Status),
                Description: r.Description,
                OccurredAt:  r.OccurredAt,
                CarrierCode: r.CarrierCode,
                TrackingCode: r.TrackingCode,
                Location:    r.Location,
                OperatorId:  r.OperatorId)).ToList();

            return events.Count > 0
                ? FulfillmentAggregate.Reconstitute(orderId, events)
                : null;
        }, ct);

    // ── SaveAsync — persiste novos eventos + order_history se final ───────────
    public Task SaveAsync(FulfillmentAggregate aggregate, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            await using var tx   = await conn.BeginTransactionAsync(token);
            try
            {
                // Etapa 6 — INSERT fulfillment_event (append-only)
                var existingCount = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM fulfillment_event WHERE order_id = @orderId",
                    new { orderId = aggregate.OrderId }, tx);

                var newEvents = aggregate.Events.Skip(existingCount).ToList();

                foreach (var evt in newEvents)
                {
                    await conn.ExecuteAsync("""
                        INSERT INTO fulfillment_event (
                            order_id, status, description, occurred_at,
                            carrier_code, tracking_code, location, operator_id
                        ) VALUES (
                            @orderId, @status, @description, @occurredAt,
                            @carrierCode, @trackingCode, @location, @operatorId
                        )
                        """,
                        new
                        {
                            orderId      = aggregate.OrderId,
                            status       = evt.Status.ToString(),
                            description  = evt.Description,
                            occurredAt   = evt.OccurredAt,
                            carrierCode  = evt.CarrierCode,
                            trackingCode = evt.TrackingCode,
                            location     = evt.Location,
                            operatorId   = evt.OperatorId
                        }, tx);
                }

                // Etapa 7 — INSERT order_history no status final
                var lastEvent = newEvents.LastOrDefault();
                if (lastEvent is not null && FinalStatuses.Contains(lastEvent.Status))
                {
                    await conn.ExecuteAsync("""
                        INSERT INTO order_history (
                            order_id, final_status, tracking_code,
                            carrier_code, events_snapshot, occurred_at
                        ) VALUES (
                            @orderId, @finalStatus, @trackingCode,
                            @carrierCode, @eventsSnapshot::jsonb, @occurredAt
                        )
                        ON CONFLICT (order_id) DO UPDATE
                            SET final_status    = EXCLUDED.final_status,
                                tracking_code   = EXCLUDED.tracking_code,
                                events_snapshot = EXCLUDED.events_snapshot,
                                occurred_at     = EXCLUDED.occurred_at
                        """,
                        new
                        {
                            orderId        = aggregate.OrderId,
                            finalStatus    = lastEvent.Status.ToString(),
                            trackingCode   = aggregate.TrackingCode,
                            carrierCode    = aggregate.CarrierCode,
                            eventsSnapshot = JsonSerializer.Serialize(aggregate.Events),
                            occurredAt     = lastEvent.OccurredAt
                        }, tx);
                }

                await tx.CommitAsync(token);
            }
            catch
            {
                await tx.RollbackAsync(token);
                throw;
            }
        }, ct);

    // ── GetHistoryAsync ───────────────────────────────────────────────────────
    public Task<IReadOnlyList<FulfillmentEvent>> GetHistoryAsync(
        string orderId, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            var rows = await conn.QueryAsync<FulfillmentEventRow>("""
                SELECT status, description, occurred_at,
                       carrier_code, tracking_code, location, operator_id
                  FROM fulfillment_event
                 WHERE order_id = @orderId
                 ORDER BY occurred_at ASC
                """, new { orderId });

            return (IReadOnlyList<FulfillmentEvent>)rows.Select(r => new FulfillmentEvent(
                OrderId:      orderId,
                Status:       Enum.Parse<FulfillmentStatus>(r.Status),
                Description:  r.Description,
                OccurredAt:   r.OccurredAt,
                CarrierCode:  r.CarrierCode,
                TrackingCode: r.TrackingCode,
                Location:     r.Location,
                OperatorId:   r.OperatorId)).ToList().AsReadOnly();
        }, ct);

    // ── ListByStatusAsync ─────────────────────────────────────────────────────
    public Task<IReadOnlyList<FulfillmentAggregate>> ListByStatusAsync(
        FulfillmentStatus status, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            var orderIds = await conn.QueryAsync<string>("""
                SELECT DISTINCT order_id
                  FROM fulfillment_event fe
                 WHERE fe.status = @status
                   AND NOT EXISTS (
                       SELECT 1 FROM fulfillment_event fe2
                        WHERE fe2.order_id = fe.order_id
                          AND fe2.occurred_at > fe.occurred_at
                   )
                """, new { status = status.ToString() });

            var result = new List<FulfillmentAggregate>();
            foreach (var orderId in orderIds)
            {
                var agg = await GetByOrderIdAsync(orderId, token);
                if (agg is not null) result.Add(agg);
            }
            return (IReadOnlyList<FulfillmentAggregate>)result.AsReadOnly();
        }, ct);

    // ── DTO interno ───────────────────────────────────────────────────────────
    private sealed record FulfillmentEventRow(
        string   Status,
        string   Description,
        DateTime OccurredAt,
        string?  CarrierCode,
        string?  TrackingCode,
        string?  Location,
        string?  OperatorId);
}
