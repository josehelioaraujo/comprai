using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Caching.Hybrid;
using UcpAgent.Domain.Entities;
using UcpAgent.Domain.Enums;
using UcpAgent.Infrastructure.Persistence;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository
{
    private readonly IDbConnectionFactory _factory;
    private readonly HybridCache?         _cache;

    public OrderRepository(IDbConnectionFactory factory, HybridCache? cache = null)
    {
        _factory = factory;
        _cache   = cache;
    }

    /// <summary>Persiste order + items + order_outbox em 1 TX com retry+CB.</summary>
    public Task SaveAsync(Order order, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await _factory.CreateAsync(token);
            await using var tx   = await conn.BeginTransactionAsync(token);
            try
            {
                var idempotencyKey = Guid.NewGuid();

                await conn.ExecuteAsync("""
                    INSERT INTO "order" (
                        id, idempotency_key, session_id,
                        status, total_amount,
                        shipping_zip, shipping_street, shipping_number,
                        shipping_complement, shipping_city, shipping_state,
                        created_at, updated_at
                    ) VALUES (
                        @id::uuid, @idempotencyKey, @sessionId::uuid,
                        @status, @total,
                        @shippingZip, @shippingStreet, @shippingNumber,
                        @shippingComplement, @shippingCity, @shippingState,
                        @createdAt, @updatedAt
                    )
                    ON CONFLICT (idempotency_key) DO NOTHING
                    """,
                    new
                    {
                        id                 = order.Id,
                        idempotencyKey,
                        sessionId          = order.SessionId,
                        status             = order.Status.ToString().ToLower(),
                        total              = order.Total,
                        shippingZip        = order.ShippingZip,
                        shippingStreet     = order.ShippingStreet,
                        shippingNumber     = order.ShippingNumber,
                        shippingComplement = order.ShippingComplement,
                        shippingCity       = order.ShippingCity,
                        shippingState      = order.ShippingState,
                        createdAt          = order.CreatedAt,
                        updatedAt          = order.UpdatedAt
                    }, tx);

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

                var payload = JsonSerializer.Serialize(new
                {
                    orderId    = order.Id,
                    sessionId  = order.SessionId,
                    status     = order.Status.ToString(),
                    total      = order.Total,
                    items      = order.Items.Select(i => new
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
                    new { key = idempotencyKey, topic = "ucp.order.created", payload }, tx);

                await tx.CommitAsync(token);
            }
            catch
            {
                await tx.RollbackAsync(token);
                throw;
            }
        }, ct);

    /// <summary>UPDATE order.status com retry+CB + invalida cache.</summary>
    public Task UpdateStatusAsync(string orderId, OrderStatus status,
        CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await _factory.CreateAsync(token);
            await conn.ExecuteAsync("""
                UPDATE "order"
                   SET status     = @status,
                       updated_at = NOW()
                 WHERE id = @id::uuid
                """,
                new { id = orderId, status = status.ToString().ToLower() });

            if (_cache is not null)
                await _cache.RemoveAsync($"order:{orderId}", token);
        }, ct);

    /// <summary>GET com HybridCache (TTL 2min) + fallback BD com retry+CB.</summary>
    public Task<OrderSummary?> GetByIdAsync(string orderId, CancellationToken ct = default)
    {
        if (_cache is not null)
        {
            return _cache.GetOrCreateAsync(
                $"order:{orderId}",
                async token => await FetchByIdAsync(orderId, token),
                new HybridCacheEntryOptions
                {
                    Expiration           = TimeSpan.FromMinutes(2),
                    LocalCacheExpiration = TimeSpan.FromSeconds(30)
                },
                cancellationToken: ct).AsTask()!;
        }
        return FetchByIdAsync(orderId, ct);
    }

    private Task<OrderSummary?> FetchByIdAsync(string orderId, CancellationToken ct)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await _factory.CreateAsync(token);
            return await conn.QuerySingleOrDefaultAsync<OrderSummary>("""
                SELECT id            AS OrderId,
                       status,
                       total_amount  AS Total,
                       tracking_code AS TrackingCode,
                       created_at
                  FROM "order"
                 WHERE id = @id::uuid
                """,
                new { id = orderId });
        }, ct);

    public sealed record OrderSummary(
        string   OrderId,
        string   Status,
        decimal  Total,
        string?  TrackingCode,
        DateTime CreatedAt);
}
