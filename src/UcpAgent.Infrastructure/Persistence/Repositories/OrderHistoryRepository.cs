using Dapper;
using UcpAgent.Infrastructure.Persistence;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

public sealed class OrderHistoryRepository(IDbConnectionFactory factory) : IOrderHistoryPort
{
    public Task InsertAsync(OrderHistoryEntry entry, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            await conn.ExecuteAsync("""
                INSERT INTO order_history (
                    order_id, customer_id, status,
                    total_amount, items_snapshot, created_at
                ) VALUES (
                    @orderId::uuid, @customerId::uuid, @status,
                    @totalAmount, @itemsSnapshot::jsonb, NOW()
                )
                ON CONFLICT (order_id) DO NOTHING
                """,
                new
                {
                    orderId       = entry.OrderId,
                    customerId    = entry.CustomerId?.ToString(),
                    status        = entry.Status,
                    totalAmount   = entry.TotalAmount,
                    itemsSnapshot = entry.ItemsSnapshot
                });
        }, ct);
}
