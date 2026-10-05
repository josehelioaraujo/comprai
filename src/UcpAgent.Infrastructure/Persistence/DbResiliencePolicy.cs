using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace UcpAgent.Infrastructure.Persistence;

/// <summary>
/// Pipeline de resiliência para operações no PostgreSQL via Dapper.
/// Retry exponencial + jitter + circuit breaker.
/// Uso: await DbResiliencePolicy.ExecuteAsync(ct => repo.SaveAsync(...), ct);
/// </summary>
public static class DbResiliencePolicy
{
    private static readonly ResiliencePipeline _pipeline = new ResiliencePipelineBuilder()
        // 1. Retry — 3 tentativas, backoff exponencial + jitter
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType      = DelayBackoffType.Exponential,
            UseJitter        = true,
            Delay            = TimeSpan.FromMilliseconds(200),
            ShouldHandle     = new PredicateBuilder()
                .Handle<Npgsql.NpgsqlException>()
                .Handle<TimeoutException>()
                .Handle<InvalidOperationException>(ex =>
                    ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
        })
        // 2. Circuit Breaker — abre após 5 falhas em 30s, espera 15s
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio      = 0.5,
            MinimumThroughput = 5,
            SamplingDuration  = TimeSpan.FromSeconds(30),
            BreakDuration     = TimeSpan.FromSeconds(15),
            ShouldHandle      = new PredicateBuilder()
                .Handle<Npgsql.NpgsqlException>()
                .Handle<TimeoutException>()
        })
        .Build();

    public static Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
        => _pipeline.ExecuteAsync(action, ct);

    public static Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
        => _pipeline.ExecuteAsync(action, ct);
}
