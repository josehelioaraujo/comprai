namespace UcpAgent.Infrastructure.Persistence;

/// <summary>
/// Resiliência para operações BD — retry exponencial + jitter + circuit breaker,
/// implementados nativamente sem dependência de Polly.
/// </summary>
public static class DbResiliencePolicy
{
    private static readonly Random _rng = Random.Shared;

    // ── Circuit Breaker state (por processo) ─────────────────────────────────
    private static int    _failureCount;
    private static bool   _circuitOpen;
    private static DateTime _openUntil = DateTime.MinValue;

    private const int    MaxRetries          = 3;
    private const int    BaseDelayMs         = 200;
    private const int    CbFailureThreshold  = 5;
    private static readonly TimeSpan CbBreakDuration = TimeSpan.FromSeconds(15);

    private static bool IsTransient(Exception ex) =>
        ex is Npgsql.NpgsqlException
            or TimeoutException
            or InvalidOperationException { Message: var m }
        when ex is not InvalidOperationException ||
             m.Contains("connection", StringComparison.OrdinalIgnoreCase);

    /// <summary>Executa ação void com retry + jitter + circuit breaker.</summary>
    public static Task ExecuteAsync(Func<CancellationToken, Task> action,
        CancellationToken ct = default)
        => ExecuteAsync(async t => { await action(t); return 0; }, ct)
            .ContinueWith(_ => { }, ct, TaskContinuationOptions.None, TaskScheduler.Default);

    /// <summary>Executa ação com retorno com retry + jitter + circuit breaker.</summary>
    public static async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action,
        CancellationToken ct = default)
    {
        // Circuit breaker — rejeita imediatamente se circuito aberto
        if (_circuitOpen && DateTime.UtcNow < _openUntil)
            throw new InvalidOperationException(
                $"[DbResiliencePolicy] Circuit aberto até {_openUntil:HH:mm:ss} — BD temporariamente indisponível.");

        if (_circuitOpen && DateTime.UtcNow >= _openUntil)
        {
            // Half-open: tenta uma vez
            _circuitOpen  = false;
            _failureCount = 0;
        }

        var attempt = 0;
        while (true)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                var result = await action(ct);

                // Sucesso — reseta falhas do CB
                Interlocked.Exchange(ref _failureCount, 0);
                return result;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (IsTransient(ex) && attempt < MaxRetries)
            {
                attempt++;
                var failures = Interlocked.Increment(ref _failureCount);

                // Abre circuito se atingiu threshold
                if (failures >= CbFailureThreshold)
                {
                    _circuitOpen = true;
                    _openUntil   = DateTime.UtcNow.Add(CbBreakDuration);
                }

                // Backoff exponencial + jitter: 200ms * 2^attempt ± 50%
                var baseMs  = BaseDelayMs * (int)Math.Pow(2, attempt);
                var jitterMs = _rng.Next(-baseMs / 2, baseMs / 2);
                var delay   = TimeSpan.FromMilliseconds(Math.Max(50, baseMs + jitterMs));
                await Task.Delay(delay, ct);
            }
            catch (Exception ex) when (!IsTransient(ex))
            {
                throw; // Não transiente — não faz retry
            }
        }
    }
}
