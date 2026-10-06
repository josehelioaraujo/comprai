using System.Text.Json;
using Dapper;
using UcpAgent.Infrastructure.Persistence;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

public sealed record PaymentRecord(
    string  OrderId,
    string  Provider,
    string  Method,
    decimal Amount,
    string? CardLast4 = null,
    string? CardBrand = null,
    string? PixKey    = null,
    string? PixQrCode = null);

public sealed class PaymentRepository
{
    private readonly IDbConnectionFactory _factory;

    public PaymentRepository(IDbConnectionFactory factory)
        => _factory = factory;

    /// <summary>INSERT payment + payment_outbox em 1 TX com retry+CB.</summary>
    public Task<Guid> ConfirmAsync(PaymentRecord record, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await _factory.CreateAsync(token);
            await using var tx   = await conn.BeginTransactionAsync(token);
            try
            {
                var idempotencyKey = Guid.NewGuid();

                var paymentId = await conn.QuerySingleAsync<Guid>("""
                    INSERT INTO payment (
                        idempotency_key, order_id, provider, method,
                        status, amount, card_last4, card_brand,
                        pix_key, pix_qr_code, paid_at
                    ) VALUES (
                        @idempotencyKey, @orderId, @provider, @method,
                        'confirmed', @amount, @cardLast4, @cardBrand,
                        @pixKey, @pixQrCode, NOW()
                    )
                    ON CONFLICT (idempotency_key) DO NOTHING
                    RETURNING id
                    """,
                    new
                    {
                        idempotencyKey,
                        orderId   = record.OrderId,
                        provider  = record.Provider,
                        method    = record.Method,
                        amount    = record.Amount,
                        cardLast4 = record.CardLast4,
                        cardBrand = record.CardBrand,
                        pixKey    = record.PixKey,
                        pixQrCode = record.PixQrCode
                    }, tx);

                var payload = JsonSerializer.Serialize(new
                {
                    paymentId,
                    orderId    = record.OrderId,
                    provider   = record.Provider,
                    method     = record.Method,
                    amount     = record.Amount,
                    occurredAt = DateTime.UtcNow
                });

                await conn.ExecuteAsync("""
                    INSERT INTO payment_outbox (idempotency_key, topic, payload)
                    VALUES (@key, @topic, @payload::jsonb)
                    ON CONFLICT (idempotency_key) DO NOTHING
                    """,
                    new { key = idempotencyKey, topic = "ucp.payment.confirmed", payload }, tx);

                await tx.CommitAsync(token);
                return paymentId;
            }
            catch
            {
                await tx.RollbackAsync(token);
                throw;
            }
        }, ct);
}
