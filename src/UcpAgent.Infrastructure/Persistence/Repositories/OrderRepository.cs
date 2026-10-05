using System.Text.Json;
using Dapper;
using Npgsql;
using UcpAgent.Domain.Entities;
using UcpAgent.Domain.Enums;
using UcpAgent.Infrastructure.Persistence;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository
{
    private readonly IDbConnectionFactory _factory;

    public OrderRepository(IDbConnectionFactory factory)
        => _factory = factory;

    /// <summary>
    /// Persiste order + items + order_outbox em uma única transação.
    /// </summary>
    public async Task SaveAsync(Order order, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection) await _factory.CreateAsync(ct);
        await using var tx   = await conn.BeginTransactionAsync(ct);

        try
        {
            var idempotencyKey = Guid.NewGuid();

            // 1. INSERT order
            await conn.ExecuteAsync("""
                INSERT INTO "order" (
                    id, idempotency_key, session_id, customer_id,
                    status, total_amount, created_at, updated_at
                ) VALUES (
                    @id::uuid, @idempotencyKey, @sessionId::uuid, NULL,
                    @status, @total, @createdAt, @updatedAt
                )
                ON CONFLICT (idempotency_key) DO NOTHING
                """,
                new
                {
                    id             = order.Id,
                    idempotencyKey,
                    sessionId      = order.SessionId,
                    status         = order.Status.ToString().ToLower(),
                    total          = order.Total,
                    createdAt      = order.CreatedAt,
                    updatedAt      = order.UpdatedAt
                }, tx);

            // 2. INSERT order_items
            foreach (var item in order.Items)
            {
                await conn.ExecuteAsync("""
                    INSERT INTO order_item (
                        order_id, product_id, product_title,
                        quantity, unit_price, source
                    ) VALUES (
                        @orderId::uuid, @productId, @productTitle,
                        @quantity, @unitPrice, @source
                    )
                    """,
                    new
                    {
                        orderId      = order.Id,
                        productId    = item.ProductId,
                        productTitle = item.ProductTitle,
                        quantity     = item.Quantity,
                        unitPrice    = item.UnitPrice,
                        source       = item.Source
                    }, tx);
            }

            // 3. INSERT order_outbox — atomico com o pedido
            var payload = JsonSerializer.Serialize(new
            {
                orderId   = order.Id,
                sessionId = order.SessionId,
                status    = order.Status.ToString(),
                total     = order.Total,
                items     = order.Items.Select(i => new
                {
                    productId = i.ProductId,
                    title     = i.ProductTitle,
                    qty       = i.Quantity,
                    price     = i.UnitPrice
                }),
                occurredAt = DateTime.UtcNow
            });

            await conn.ExecuteAsync("""
                INSERT INTO order_outbox (idempotency_key, topic, payload)
                VALUES (@key, @topic, @payload::jsonb)
                ON CONFLICT (idempotency_key) DO NOTHING
                """,
                new
                {
                    key     = idempotencyKey,
                    topic   = "ucp.order.created",
                    payload
                }, tx);

            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task UpdateStatusAsync(string orderId, OrderStatus status,
        CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateAsync(ct);
        await conn.ExecuteAsync("""
            UPDATE "order"
               SET status     = @status,
                   updated_at = NOW()
             WHERE id = @id::uuid
            """,
            new { id = orderId, status = status.ToString().ToLower() });
    }

    public async Task<OrderStatusDto?> GetStatusAsync(string orderId,
        CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateAsync(ct);
        return await conn.QuerySingleOrDefaultAsync<OrderStatusDto>("""
            SELECT o.id         AS OrderId,
                   o.status,
                   o.total_amount AS Total,
                   o.tracking_code AS TrackingCode,
                   o.created_at
              FROM "order" o
             WHERE o.id = @id::uuid
            """,
            new { id = orderId });
    }
}
