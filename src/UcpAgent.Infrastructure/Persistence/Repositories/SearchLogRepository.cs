using Dapper;
using UcpAgent.Infrastructure.Persistence;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

public sealed class SearchLogRepository(IDbConnectionFactory factory) : ISearchLogPort
{
    public Task LogAsync(SearchLogEntry entry, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            await using var conn = await factory.CreateAsync(token);
            await conn.ExecuteAsync("""
                INSERT INTO search_log (
                    query, result_count, sources,
                    duration_ms, session_id, occurred_at
                ) VALUES (
                    @query, @resultCount, @sources,
                    @durationMs, @sessionId::uuid, @occurredAt
                )
                """,
                new
                {
                    query       = entry.Query,
                    resultCount = entry.ResultCount,
                    sources     = entry.Sources,
                    durationMs  = entry.DurationMs,
                    sessionId   = entry.SessionId,
                    occurredAt  = entry.OccurredAt
                });
        }, ct);
}