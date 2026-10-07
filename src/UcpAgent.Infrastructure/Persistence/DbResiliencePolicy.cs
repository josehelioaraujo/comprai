namespace UcpAgent.Infrastructure.Persistence;

/// <summary>
/// Resiliência para operações BD — retry exponencial + jitter + circuit breaker,
/// implementados nativamente sem dependência de Polly.
/// </summary>
public static class DbResiliencePolicy
{
    private static readonly Random _rng = Random.Shared;

    // Circuit Breaker state
    private static int      _failureCount;
    private static bool     _circuitOpen;
    private static DateTime _openUntil = DateTime.MinValue;

    private const int MaxRetries         = 3;
    private const int BaseDelayMs        = 200;
    private const int CbFailureThreshold = 5;
    private static readonly TimeSpan CbBreakDuration = TimeSpan.FromSeconds(15);

    private static bool IsTransient(Exception ex)
    {
        if (ex is Npgsql.NpgsqlException) return true;
        if (ex is TimeoutException) return true;
        if (ex is InvalidOperationException ioe &&
            ioe.Message.Contains("connection", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>Executa ação void com retry + jitter + circuit breaker.</summary>
    public static async Task ExecuteAsync(Func<CancellationToken, Task> action,
        CancellationToken ct = default)
    {
        await ExecuteAsync(async token =>
        {
            await action(token);
            return 0;
        }, ct);
    }

    /// <summary>Executa ação com retorno com retry + jitter + circuit breaker.</summary>
    public static async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action,
        CancellationToken ct = default)
    {
        // Circuit breaker — rejeita imediatamente se circuito aberto
        if (_circuitOpen && DateTime.UtcNow < _openUntil)
            throw new InvalidOperationException(
                $"[DbResiliencePolicy] Circuit aberto até {_openUntil:HH:mm:ss}.");

        // Half-open: tenta novamente após break duration
        if (_circuitOpen && DateTime.UtcNow >= _openUntil)
        {
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
                Interlocked.Exchange(ref _failureCount, 0);
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < MaxRetries)
            {
                attempt++;
                var failures = Interlocked.Increment(ref _failureCount);

                if (failures >= CbFailureThreshold)
                {
                    _circuitOpen = true;
                    _openUntil   = DateTime.UtcNow.Add(CbBreakDuration);
                }

                // Backoff exponencial + jitter
                var baseMs   = BaseDelayMs * (int)Math.Pow(2, attempt);
                var jitterMs = _rng.Next(-baseMs / 2, baseMs / 2);
                var delayMs  = Math.Max(50, baseMs + jitterMs);
                await Task.Delay(delayMs, ct);
            }
        }
    }
}
