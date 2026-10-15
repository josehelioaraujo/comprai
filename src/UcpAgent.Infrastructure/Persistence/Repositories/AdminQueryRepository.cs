using System.Diagnostics.CodeAnalysis;
using Dapper;
using UcpAgent.Infrastructure.Persistence;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

// ââ DTOs âââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ

public sealed record AdminSummaryDto(
    long    TotalOrders,
    decimal TotalRevenue,
    long    TotalCustomers,
    long    PendingOrders,
    long    CancelledOrders,
    decimal AvgOrderValue);

public sealed record AdminOrderRow(
    Guid    OrderId,
    string  Status,
    decimal Total,
    string? CustomerName,
    string? CustomerEmail,
    string? PaymentMethod,
    long    ItemCount,
    string? FirstProductTitle,
    string? FirstProductSku,
    DateTime CreatedAt);

public sealed record AdminOrderDetailRow(
    string   OrderId,
    string   Status,
    decimal  Total,
    string?  CustomerName,
    string?  CustomerEmail,
    string?  CustomerPhone,
    string?  TrackingCode,
    string?  CancelReason,
    string?  PaymentMethod,
    string?  PaymentStatus,
    decimal? PaymentAmount,
    string?  ShippingZip,
    string?  ShippingStreet,
    string?  ShippingNumber,
    string?  ShippingCity,
    string?  ShippingState,
    DateTime CreatedAt);

public sealed record AdminOrderDetailDto(
    string   OrderId,
    string   Status,
    decimal  Total,
    string?  CustomerName,
    string?  CustomerEmail,
    string?  CustomerPhone,
    string?  TrackingCode,
    string?  CancelReason,
    string?  PaymentMethod,
    string?  PaymentStatus,
    decimal? PaymentAmount,
    string?  ShippingZip,
    string?  ShippingStreet,
    string?  ShippingNumber,
    string?  ShippingCity,
    string?  ShippingState,
    DateTime CreatedAt,
    IReadOnlyList<AdminOrderItemDto> Items);

public sealed record AdminOrderItemDto(
    string  ProductId,
    string  ProductTitle,
    string? Sku,
    int     Quantity,
    decimal UnitPrice,
    string? ImageUrl);

public sealed record AdminPaymentsStatsDto(
    long    TotalPayments,
    decimal TotalConfirmed,
    decimal TotalPending,
    decimal TotalFailed,
    long    PixCount,
    long    CreditCardCount,
    long    DebitCardCount);

public sealed record AdminFulfillmentStatsDto(
    long TotalShipped,
    long TotalDelivered,
    long TotalCancelled,
    long AwaitingFulfillment);

public sealed record AdminCustomersStatsDto(
    long TotalCustomers,
    long VerifiedCustomers,
    long WithTotpCustomers,
    long NewThisMonth);

public sealed record AdminSalesChartPoint(
    string  Date,
    decimal Revenue,
    long    Orders);

public sealed record AdminOrderPage(
    IReadOnlyList<AdminOrderRow> Items,
    long Total);

public sealed record AdminTopProductDto(
    string  ProductId,
    string  ProductTitle,
    string? ImageUrl,
    long    TotalQuantity,
    decimal TotalRevenue);

public sealed record AdminStatusBreakdownDto(
    string  Status,
    long    Count,
    decimal Total);

public sealed record AdminPaymentBreakdownDto(
    string  Method,
    long    Count,
    decimal Total);

// ââ Repository âââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ

[ExcludeFromCodeCoverage]
public sealed class AdminQueryRepository(IDbConnectionFactory factory)
{
    public Task<AdminSummaryDto> GetSummaryAsync(CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            return await conn.QuerySingleAsync<AdminSummaryDto>("""
                SELECT
                    COUNT(*)                                          AS TotalOrders,
                    COALESCE(SUM(total_amount), 0)                   AS TotalRevenue,
                    (SELECT COUNT(*) FROM customer)                  AS TotalCustomers,
                    COUNT(*) FILTER (WHERE status = 'pending')       AS PendingOrders,
                    COUNT(*) FILTER (WHERE status = 'cancelled')     AS CancelledOrders,
                    COALESCE(AVG(total_amount), 0)                   AS AvgOrderValue
                FROM "order"
                """);
        }, ct);

    public Task<AdminOrderPage> GetOrdersAsync(
        int page = 1, int pageSize = 20, string? status = null, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);

            var total = await conn.ExecuteScalarAsync<long>("""
                SELECT COUNT(DISTINCT o.id)
                FROM "order" o
                WHERE (@Status::text IS NULL OR o.status = @Status)
                """,
                new { Status = status });

            var rows = await conn.QueryAsync<AdminOrderRow>("""
                SELECT o.id                  AS OrderId,
                       o.status              AS Status,
                       o.total_amount        AS Total,
                       c.name                AS CustomerName,
                       c.email               AS CustomerEmail,
                       p.method              AS PaymentMethod,
                       COUNT(oi.id)          AS ItemCount,
                       first_item.product_title AS FirstProductTitle,
                       first_item.sku           AS FirstProductSku,
                       o.created_at          AS CreatedAt
                FROM "order" o
                LEFT JOIN customer   c  ON c.id = o.customer_id
                LEFT JOIN payment    p  ON p.order_id = o.id
                LEFT JOIN order_item oi ON oi.order_id = o.id
                LEFT JOIN LATERAL (
                    SELECT product_title, sku
                    FROM order_item
                    WHERE order_id = o.id
                    ORDER BY created_at
                    LIMIT 1
                ) first_item ON true
                WHERE (@Status::text IS NULL OR o.status = @Status)
                GROUP BY o.id, c.name, c.email, p.method, first_item.product_title, first_item.sku
                ORDER BY o.created_at DESC
                LIMIT @PageSize OFFSET @Offset
                """,
                new { Status = status, PageSize = pageSize, Offset = (page - 1) * pageSize });

            return new AdminOrderPage(rows.ToList(), total);
        }, ct);

    public Task<AdminOrderDetailDto?> GetOrderDetailAsync(string orderId, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);

            var orderRow = await conn.QuerySingleOrDefaultAsync<AdminOrderDetailRow>("""
                SELECT o.id::TEXT        AS OrderId,
                       o.status          AS Status,
                       o.total_amount    AS Total,
                       c.name            AS CustomerName,
                       c.email           AS CustomerEmail,
                       c.phone           AS CustomerPhone,
                       o.tracking_code   AS TrackingCode,
                       o.cancel_reason   AS CancelReason,
                       p.method          AS PaymentMethod,
                       p.status          AS PaymentStatus,
                       p.amount          AS PaymentAmount,
                       o.shipping_zip    AS ShippingZip,
                       o.shipping_street AS ShippingStreet,
                       o.shipping_number AS ShippingNumber,
                       o.shipping_city   AS ShippingCity,
                       o.shipping_state  AS ShippingState,
                       o.created_at      AS CreatedAt
                FROM "order" o
                LEFT JOIN customer c ON c.id = o.customer_id
                LEFT JOIN payment  p ON p.order_id = o.id
                WHERE o.id = @OrderId::uuid
                """,
                new { OrderId = orderId });

            if (orderRow is null) return null;

            var items = (await conn.QueryAsync<AdminOrderItemDto>("""
                SELECT product_id    AS ProductId,
                       product_title AS ProductTitle,
                       sku           AS Sku,
                       quantity      AS Quantity,
                       unit_price    AS UnitPrice,
                       image_url     AS ImageUrl
                FROM order_item
                WHERE order_id = @OrderId::uuid
                """,
                new { OrderId = orderId })).ToList();

            return new AdminOrderDetailDto(
                orderRow.OrderId,
                orderRow.Status,
                orderRow.Total,
                orderRow.CustomerName,
                orderRow.CustomerEmail,
                orderRow.CustomerPhone,
                orderRow.TrackingCode,
                orderRow.CancelReason,
                orderRow.PaymentMethod,
                orderRow.PaymentStatus,
                orderRow.PaymentAmount,
                orderRow.ShippingZip,
                orderRow.ShippingStreet,
                orderRow.ShippingNumber,
                orderRow.ShippingCity,
                orderRow.ShippingState,
                orderRow.CreatedAt,
                items);
        }, ct);

    public Task<AdminPaymentsStatsDto> GetPaymentsStatsAsync(CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            return await conn.QuerySingleAsync<AdminPaymentsStatsDto>("""
                SELECT
                    COUNT(*)                                                  AS TotalPayments,
                    COALESCE(SUM(amount) FILTER (WHERE status='confirmed'),0) AS TotalConfirmed,
                    COALESCE(SUM(amount) FILTER (WHERE status='pending'),0)   AS TotalPending,
                    COALESCE(SUM(amount) FILTER (WHERE status='failed'),0)    AS TotalFailed,
                    COUNT(*) FILTER (WHERE method='pix')                      AS PixCount,
                    COUNT(*) FILTER (WHERE method='credit_card')              AS CreditCardCount,
                    COUNT(*) FILTER (WHERE method='debit_card')               AS DebitCardCount
                FROM payment
                """);
        }, ct);

    public Task<AdminFulfillmentStatsDto> GetFulfillmentStatsAsync(CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            return await conn.QuerySingleAsync<AdminFulfillmentStatsDto>("""
                SELECT
                    COUNT(*) FILTER (WHERE status = 'shipped')    AS TotalShipped,
                    COUNT(*) FILTER (WHERE status = 'delivered')  AS TotalDelivered,
                    COUNT(*) FILTER (WHERE status = 'cancelled')  AS TotalCancelled,
                    (SELECT COUNT(*) FROM "order"
                     WHERE status IN ('confirmed','processing'))   AS AwaitingFulfillment
                FROM "order"
                """);
        }, ct);

    public Task<AdminCustomersStatsDto> GetCustomersStatsAsync(CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            return await conn.QuerySingleAsync<AdminCustomersStatsDto>("""
                SELECT
                    COUNT(*)                                                        AS TotalCustomers,
                    COUNT(*) FILTER (WHERE email_verified = TRUE)                  AS VerifiedCustomers,
                    COUNT(*) FILTER (WHERE totp_enabled = TRUE)                    AS WithTotpCustomers,
                    COUNT(*) FILTER (WHERE created_at >= date_trunc('month', NOW())) AS NewThisMonth
                FROM customer
                WHERE role = 'customer'
                """);
        }, ct);

    public Task<IReadOnlyList<AdminSalesChartPoint>> GetSalesChartAsync(int days = 30, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            var rows = await conn.QueryAsync<AdminSalesChartPoint>("""
                SELECT
                    TO_CHAR(date_trunc('day', o.created_at), 'YYYY-MM-DD') AS Date,
                    COALESCE(SUM(o.total_amount), 0)                        AS Revenue,
                    COUNT(o.id)                                             AS Orders
                FROM "order" o
                WHERE o.created_at >= NOW() - (@Days || ' days')::interval
                  AND o.status NOT IN ('cancelled')
                GROUP BY date_trunc('day', o.created_at)
                ORDER BY date_trunc('day', o.created_at)
                """,
                new { Days = days });
            return (IReadOnlyList<AdminSalesChartPoint>)rows.ToList();
        }, ct);

    public Task<IReadOnlyList<AdminStatusBreakdownDto>> GetStatusBreakdownAsync(CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            var rows = await conn.QueryAsync<AdminStatusBreakdownDto>("""
                SELECT
                    status                      AS Status,
                    COUNT(*)                    AS Count,
                    COALESCE(SUM(total_amount), 0) AS Total
                FROM "order"
                GROUP BY status
                ORDER BY Count DESC
                """);
            return (IReadOnlyList<AdminStatusBreakdownDto>)rows.ToList();
        }, ct);

    public Task<IReadOnlyList<AdminPaymentBreakdownDto>> GetPaymentBreakdownAsync(CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            var rows = await conn.QueryAsync<AdminPaymentBreakdownDto>("""
                SELECT
                    method                                                        AS Method,
                    COUNT(*)                                                      AS Count,
                    COALESCE(SUM(amount) FILTER (WHERE status = 'confirmed'), 0) AS Total
                FROM payment
                GROUP BY method
                ORDER BY Count DESC
                """);
            return (IReadOnlyList<AdminPaymentBreakdownDto>)rows.ToList();
        }, ct);

    public Task<IReadOnlyList<AdminTopProductDto>> GetTopProductsAsync(int limit = 10, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            var rows = await conn.QueryAsync<AdminTopProductDto>("""
                SELECT
                    oi.product_id    AS ProductId,
                    oi.product_title AS ProductTitle,
                    oi.image_url     AS ImageUrl,
                    SUM(oi.quantity) AS TotalQuantity,
                    SUM(oi.quantity * oi.unit_price) AS TotalRevenue
                FROM order_item oi
                JOIN "order" o ON o.id = oi.order_id
                WHERE o.status NOT IN ('cancelled')
                GROUP BY oi.product_id, oi.product_title, oi.image_url
                ORDER BY TotalQuantity DESC
                LIMIT @Limit
                """,
                new { Limit = limit });
            return (IReadOnlyList<AdminTopProductDto>)rows.ToList();
        }, ct);
}
