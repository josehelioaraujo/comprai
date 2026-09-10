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

        builder.AddResilienceHandler("catalog-pipeline", pipeline =>
        {
            // 1⃣ Timeout — cancela request se demorar demais
            pipeline.AddTimeout(TimeSpan.FromSeconds(options.Timeout.TimeoutSeconds));

            // 2⃣ Retry — backoff exponencial + jitter (pulado se MaxAttempts <= 0)
            if (options.Retry.MaxAttempts > 0)
            {
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
            }

            // 3⃣ Circuit Breaker — abre o circuito quando taxa de falha excede o limiar
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

        return builder;
    }
}
