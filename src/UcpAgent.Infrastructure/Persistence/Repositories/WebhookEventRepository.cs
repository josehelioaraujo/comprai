using Dapper;
using UcpAgent.Infrastructure.Persistence;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

public sealed class WebhookEventRepository(IDbConnectionFactory factory) : IWebhookEventPort
{
    public Task<bool> TryRecordAsync(string provider, string externalId,
        string eventType, string payloadJson, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);

            // INSERT … ON CONFLICT DO NOTHING — retorna 1 linha se inseriu, 0 se duplicado
            var rows = await conn.ExecuteAsync("""
                INSERT INTO webhook_event (provider, external_id, event_type, payload)
                VALUES (@provider, @externalId, @eventType, @payload::jsonb)
                ON CONFLICT (provider, external_id) DO NOTHING
                """,
                new
                {
                    provider,
                    externalId,
                    eventType,
                    payload = payloadJson
                });

            return rows > 0; // true = novo; false = duplicado
        }, ct);
}
