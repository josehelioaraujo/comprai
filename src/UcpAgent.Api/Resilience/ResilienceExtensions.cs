using Microsoft.Extensions.Http.Resilience;
using System.Diagnostics.CodeAnalysis;

namespace UcpAgent.Api.Resilience;

[ExcludeFromCodeCoverage]
public static class ResilienceExtensions
{
    public static IHttpClientBuilder AddCatalogResilience(
        this IHttpClientBuilder builder,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection("Resilience")
            .Get<ResilienceOptions>() ?? new ResilienceOptions();

        builder.AddResilienceHandler("catalog-pipeline", pipeline =>
        {
            // 1⃣ Timeout — cancela request se demorar demais
            pipeline.AddTimeout(TimeSpan.FromSeconds(options.Timeout.TimeoutSeconds));

            // 2⃣ Retry — backoff exponencial + jitter
            //   BackoffType e UseJitter omitidos: defaults de HttpRetryStrategyOptions
            //   ja sao Exponential + true respectivamente.
            //   ShouldHandle default cobre HttpRequestException + 5xx + 408 + 429.
            if (options.Retry.MaxAttempts > 0)
            {
                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = options.Retry.MaxAttempts,
                    Delay            = TimeSpan.FromSeconds(options.Retry.BaseDelaySeconds)
                });
            }

            // 3⃣ Circuit Breaker — abre quando taxa de falha excede o limiar
            //   ShouldHandle default cobre HttpRequestException + 5xx.
            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio      = options.CircuitBreaker.FailureRatio,
                MinimumThroughput = options.CircuitBreaker.MinimumThroughput,
                SamplingDuration  = TimeSpan.FromSeconds(options.CircuitBreaker.SamplingDurationSeconds),
                BreakDuration     = TimeSpan.FromSeconds(options.CircuitBreaker.BreakDurationSeconds)
            });
        });

        return builder;
    }
}
