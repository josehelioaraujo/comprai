using Dapper;
using UcpAgent.Infrastructure.Persistence;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

public sealed class SessionRepository(IDbConnectionFactory factory) : ISessionPort
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(2);

    public Task<Guid> GetOrCreateAsync(string? clientSessionId, string channel = "web",
        CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);

            if (Guid.TryParse(clientSessionId, out var clientGuid))
            {
                var existing = await conn.QuerySingleOrDefaultAsync<Guid?>(
                    "SELECT id FROM session WHERE id = @id AND expires_at > NOW()",
                    new { id = clientGuid });

                if (existing.HasValue)
                    return existing.Value;
            }

            var newId = Guid.NewGuid();
            await conn.ExecuteAsync("""
                INSERT INTO session (id, channel, expires_at)
                VALUES (@id, @channel, @expiresAt)
                ON CONFLICT (id) DO UPDATE SET expires_at = EXCLUDED.expires_at
                """,
                new { id = newId, channel, expiresAt = DateTime.UtcNow.Add(Ttl) });

            return newId;
        }, ct);

    public Task LinkCustomerAsync(Guid sessionId, Guid customerId,
        CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            await conn.ExecuteAsync(
                "UPDATE session SET customer_id = @customerId WHERE id = @sessionId",
                new { sessionId, customerId });
        }, ct);
}
