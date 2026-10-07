using Dapper;
using UcpAgent.Infrastructure.Persistence;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

public sealed record OutboxMessage(
    Guid    Id,
    string  Topic,
    string  Payload,
    int     RetryCount);

public sealed class OutboxRepository
{
    private readonly IDbConnectionFactory _factory;

    public OutboxRepository(IDbConnectionFactory factory)
        => _factory = factory;

    // ── Order Outbox ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingOrdersAsync(
        int batchSize = 50, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateAsync(ct);
        var rows = await conn.QueryAsync<OutboxMessage>("""
            SELECT id, topic, payload::text AS Payload, retry_count AS RetryCount
              FROM order_outbox
             WHERE status IN ('pending','failed')
               AND next_retry_at <= NOW()
             ORDER BY created_at
             LIMIT @batchSize
            FOR UPDATE SKIP LOCKED
            """, new { batchSize });
        return rows.ToList();
    }

    public async Task MarkOrderSentAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateAsync(ct);
        await conn.ExecuteAsync("""
            UPDATE order_outbox
               SET status  = 'sent',
                   sent_at = NOW()
             WHERE id = @id
            """, new { id });
    }

    public async Task MarkOrderFailedAsync(Guid id, string error,
        TimeSpan? backoff = null, CancellationToken ct = default)
    {
        var delay = backoff ?? TimeSpan.FromSeconds(30);
        await using var conn = await _factory.CreateAsync(ct);
        await conn.ExecuteAsync("""
            UPDATE order_outbox
               SET status        = 'failed',
                   retry_count   = retry_count + 1,
                   last_error    = @error,
                   next_retry_at = NOW() + @delay
             WHERE id = @id
            """, new { id, error, delay });
    }

    // ── Payment Outbox ────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingPaymentsAsync(
        int batchSize = 50, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateAsync(ct);
        var rows = await conn.QueryAsync<OutboxMessage>("""
            SELECT id, topic, payload::text AS Payload, retry_count AS RetryCount
              FROM payment_outbox
             WHERE status IN ('pending','failed')
               AND next_retry_at <= NOW()
             ORDER BY created_at
             LIMIT @batchSize
            FOR UPDATE SKIP LOCKED
            """, new { batchSize });
        return rows.ToList();
    }

    public async Task MarkPaymentSentAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateAsync(ct);
        await conn.ExecuteAsync("""
            UPDATE payment_outbox
               SET status  = 'sent',
                   sent_at = NOW()
             WHERE id = @id
            """, new { id });
    }

    public async Task MarkPaymentFailedAsync(Guid id, string error,
        TimeSpan? backoff = null, CancellationToken ct = default)
    {
        var delay = backoff ?? TimeSpan.FromSeconds(30);
        await using var conn = await _factory.CreateAsync(ct);
        await conn.ExecuteAsync("""
            UPDATE payment_outbox
               SET status        = 'failed',
                   retry_count   = retry_count + 1,
                   last_error    = @error,
                   next_retry_at = NOW() + @delay
             WHERE id = @id
            """, new { id, error, delay });
    }

    // ── Notification Outbox ───────────────────────────────────────────────────

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingNotificationsAsync(
        int batchSize = 50, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateAsync(ct);
        var rows = await conn.QueryAsync<OutboxMessage>("""
            SELECT id, queue AS Topic, payload::text AS Payload, retry_count AS RetryCount
              FROM notification_outbox
             WHERE status IN ('pending','failed')
               AND next_retry_at <= NOW()
             ORDER BY created_at
             LIMIT @batchSize
            FOR UPDATE SKIP LOCKED
            """, new { batchSize });
        return rows.ToList();
    }

    public async Task MarkNotificationSentAsync(Guid id, CancellationToken ct = default)
    {
        await using var conn = await _factory.CreateAsync(ct);
        await conn.ExecuteAsync("""
            UPDATE notification_outbox
               SET status  = 'sent',
                   sent_at = NOW()
             WHERE id = @id
            """, new { id });
    }

    public async Task MarkNotificationFailedAsync(Guid id, string error,
        TimeSpan? backoff = null, CancellationToken ct = default)
    {
        var delay = backoff ?? TimeSpan.FromSeconds(30);
        await using var conn = await _factory.CreateAsync(ct);
        await conn.ExecuteAsync("""
            UPDATE notification_outbox
               SET status        = 'failed',
                   retry_count   = retry_count + 1,
                   last_error    = @error,
                   next_retry_at = NOW() + @delay
             WHERE id = @id
            """, new { id, error, delay });
    }
}
