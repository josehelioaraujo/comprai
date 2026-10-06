using Dapper;
using UcpAgent.Infrastructure.Persistence;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

public sealed class SearchLogRepository(
    IDbConnectionFactory factory,
    ISessionPort? sessionPort = null) : ISearchLogPort
{
    public Task LogAsync(SearchLogEntry entry, CancellationToken ct = default)
        => DbResiliencePolicy.ExecuteAsync(async token =>
        {
            Guid? resolvedSessionId = null;

            // Resolve session UUID no BD (fire-and-forget safe — já estamos em background)
            if (sessionPort is not null && entry.SessionId is not null)
            {
                try
                {
                    resolvedSessionId = await sessionPort.GetOrCreateAsync(
                        entry.SessionId, ct: token);
                }
                catch
                {
                    // session opcional — não bloqueia o log
                }
            }

            await using var conn = await factory.CreateAsync(token);

            // sources: TEXT[] no PostgreSQL — converter string "A,B,C" para array
            var sourcesArray = entry.Sources
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            await conn.ExecuteAsync("""
                INSERT INTO search_log (
                    session_id, query, results_count,
                    sources, duration_ms, created_at
                ) VALUES (
                    @sessionId::uuid, @query, @resultsCount,
                    @sources, @durationMs, @createdAt
                )
                """,
                new
                {
                    sessionId    = resolvedSessionId?.ToString(),
                    query        = entry.Query,
                    resultsCount = entry.ResultCount,
                    sources      = sourcesArray,
                    durationMs   = entry.DurationMs,
                    createdAt    = entry.OccurredAt
                });
        }, ct);
}
