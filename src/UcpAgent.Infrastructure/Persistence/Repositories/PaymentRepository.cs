using System.Text.Json;
using Dapper;
using Npgsql;
using UcpAgent.Infrastructure.Persistence;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

public sealed record PaymentRecord(
    string  OrderId,
    string  Provider,
    string  Method,
    decimal Amount,
    string? CardLast4  = null,
    string? CardBrand  = null,
    string? PixKey     = null,
    string? PixQrCode  = null);

public sealed class PaymentRepository
{
    private readonly IDbConnectionFactory _factory;

    public PaymentRepository(IDbConnectionFactory factory)
        => _factory = factory;

    /// <summary>
    /// INSERT payment + INSERT payment_outbox em uma única transação.
    /// </summary>
    public async Task<Guid> ConfirmAsync(PaymentRecord record, CancellationToken ct = default)
    {
        await using var conn = (NpgsqlConnection) await _factory.CreateAsync(ct);
        await using var tx   = await conn.BeginTransactionAsync(ct);

        try
        {
            var idempotencyKey = Guid.NewGuid();

            // 1. INSERT payment
            var paymentId = await conn.QuerySingleAsync<Guid>("""
                INSERT INTO payment (
                    idempotency_key, order_id, provider, method,
                    status, amount, card_last4, card_brand,
                    pix_key, pix_qr_code, paid_at
                ) VALUES (
                    @idempotencyKey, @orderId::uuid, @provider, @method,
                    'confirmed', @amount, @cardLast4, @cardBrand,
                    @pixKey, @pixQrCode, NOW()
                )
                ON CONFLICT (idempotency_key) DO NOTHING
                RETURNING id
                """,
                new
                {
                    idempotencyKey,
                    orderId    = record.OrderId,
                    provider   = record.Provider,
                    method     = record.Method,
                    amount     = record.Amount,
                    cardLast4  = record.CardLast4,
                    cardBrand  = record.CardBrand,
                    pixKey     = record.PixKey,
                    pixQrCode  = record.PixQrCode
                }, tx);

            // 2. INSERT payment_outbox — atomico com o pagamento
            var payload = JsonSerializer.Serialize(new
            {
                paymentId  = paymentId,
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
                new
                {
                    key     = idempotencyKey,
                    topic   = "ucp.payment.confirmed",
                    payload
                }, tx);

            await tx.CommitAsync(ct);
            return paymentId;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
