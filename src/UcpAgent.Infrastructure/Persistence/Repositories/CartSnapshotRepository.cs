using System.Text.Json;
using Dapper;
using UcpAgent.Infrastructure.Persistence;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

public sealed class CartSnapshotRepository(IDbConnectionFactory factory) : ICartSnapshotPort
{
    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    public Task SaveAsync(string sessionId, IReadOnlyList<CartItemDto> items,
        CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            var total    = items.Sum(i => i.Subtotal);
            var itemsJson = JsonSerializer.Serialize(items, JsonOpts);

            await conn.ExecuteAsync("""
                INSERT INTO cart_snapshot (session_id, items, total_amount, updated_at)
                VALUES (@sessionId::uuid, @items::jsonb, @total, NOW())
                ON CONFLICT (session_id) DO UPDATE
                    SET items        = EXCLUDED.items,
                        total_amount = EXCLUDED.total_amount,
                        updated_at   = NOW()
                """,
                new { sessionId, items = itemsJson, total });
        }, ct);

    public Task<CartSnapshotDto?> GetAsync(string sessionId, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            var row = await conn.QuerySingleOrDefaultAsync<CartSnapshotRow>("""
                SELECT session_id, items, total_amount, updated_at
                  FROM cart_snapshot
                 WHERE session_id = @sessionId::uuid
                """,
                new { sessionId });

            if (row is null) return null;

            var itemList = JsonSerializer.Deserialize<List<CartItemDto>>(row.Items, JsonOpts)
                ?? [];

            return new CartSnapshotDto(
                SessionId:   row.SessionId,
                Items:       itemList,
                TotalAmount: row.TotalAmount,
                UpdatedAt:   row.UpdatedAt);
        }, ct);

    public Task DeleteAsync(string sessionId, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            await conn.ExecuteAsync(
                "DELETE FROM cart_snapshot WHERE session_id = @sessionId::uuid",
                new { sessionId });
        }, ct);

    private sealed record CartSnapshotRow(
        string   SessionId,
        string   Items,
        decimal  TotalAmount,
        DateTime UpdatedAt);
}
