using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace UcpAgent.Api.Resilience;

public static class ResilienceExtensions
{
    public static IHttpClientBuilder AddCatalogResilience(
        this IHttpClientBuilder builder,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection("Resilience")
            .Get<ResilienceOptions>() ?? new ResilienceOptions();

        return builder.AddResilienceHandler("catalog-pipeline", pipeline =>
        {
            // 1️⃣ Timeout — cancela request lento
            pipeline.AddTimeout(TimeSpan.FromSeconds(options.Timeout.TimeoutSeconds));

            // 2️⃣ Retry — backoff exponencial + jitter
            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = options.Retry.MaxAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(options.Retry.BaseDelaySeconds),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r =>
                        (int)r.StatusCode >= 500 ||
                        r.StatusCode == System.Net.HttpStatusCode.RequestTimeout ||
                        r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            });

            // 3️⃣ Circuit Breaker — abre quando taxa de falha excede o limiar
            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = options.CircuitBreaker.FailureRatio,
                MinimumThroughput = options.CircuitBreaker.MinimumThroughput,
                SamplingDuration = TimeSpan.FromSeconds(options.CircuitBreaker.SamplingDurationSeconds),
                BreakDuration = TimeSpan.FromSeconds(options.CircuitBreaker.BreakDurationSeconds),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(r => (int)r.StatusCode >= 500)
            });
        });
    }
}
