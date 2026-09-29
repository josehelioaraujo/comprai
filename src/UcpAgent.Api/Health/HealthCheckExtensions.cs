using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using System.Net.Sockets;

namespace UcpAgent.Api.Health;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddStatusPageHealthChecks(
        this IServiceCollection services,
        IConfiguration config)
    {
        // Garante que IConnectionMultiplexer? resolve como null se não registrado
        // (evita InvalidOperationException nos testes sem Redis)
        if (!services.Any(s => s.ServiceType == typeof(IConnectionMultiplexer)))
            services.AddSingleton<IConnectionMultiplexer?>(_ => null);

        services.AddHealthChecks()
            .AddCheck<RedisHealthCheck> ("redis",        tags: ["infra"])
            .AddCheck<OllamaHealthCheck>("ollama",       tags: ["ai"])
            .AddCheck("rabbitmq",     RabbitMqCheck(config),    tags: ["messaging"])
            .AddCheck("kafka",        KafkaCheck(config),       tags: ["messaging"])
            .AddCheck("datadog-otel", DatadogCheck(),           tags: ["observability"])
            .AddCheck("prometheus",   PrometheusCheck(),        tags: ["observability"])
            .AddCheck("loki",         LokiCheck(),              tags: ["observability"])
            .AddCheck("jaeger",       JaegerCheck(),            tags: ["observability"]);

        return services;
    }

    // ── Redis: usa IConnectionMultiplexer singleton — sem nova conexão ───────
    // IConnectionMultiplexer? (nullable) — retorna Degraded se não configurado
    public class RedisHealthCheck(IConnectionMultiplexer? mux) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        {
            if (mux is null)
                return Task.FromResult(HealthCheckResult.Degraded("Redis não configurado"));
            return Task.FromResult(mux.IsConnected
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Not connected"));
        }
    }

    // ── Ollama: async real, sem sync-over-async ───────────────────────────────
    [ExcludeFromCodeCoverage(Justification = "Requer Ollama em execução")]
    public class OllamaHealthCheck(IConfiguration config, IHttpClientFactory factory) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        {
            try
            {
                var baseUrl = config["Ollama__BaseUrl"] ?? config["Ollama:BaseUrl"] ?? "http://localhost:11434";
                var client  = factory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(2);
                var res = await client.GetAsync($"{baseUrl.TrimEnd('/')}/api/tags", ct);
                return res.IsSuccessStatusCode
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Degraded($"HTTP {(int)res.StatusCode}");
            }
            catch (Exception ex) { return HealthCheckResult.Degraded(ex.Message); }
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Requer RabbitMQ em execução")]
    private static Func<HealthCheckResult> RabbitMqCheck(IConfiguration config) => () =>
        TcpCheck(config["RabbitMQ:Host"] ?? "localhost", 5672, "RabbitMQ");

    [ExcludeFromCodeCoverage(Justification = "Requer Kafka em execução")]
    private static Func<HealthCheckResult> KafkaCheck(IConfiguration config) => () =>
    {
        var bs   = config["Kafka:BootstrapServers"] ?? "localhost:9092";
        var host = bs.Split(':')[0];
        var port = int.TryParse(bs.Split(':').ElementAtOrDefault(1), out var p) ? p : 9092;
        return TcpCheck(host, port, "Kafka");
    };

    [ExcludeFromCodeCoverage(Justification = "Requer OTel Collector em execução")]
    private static Func<HealthCheckResult> DatadogCheck() => () =>
        TcpCheck("comprai-otel-collector", 4317, "OTel Collector");

    [ExcludeFromCodeCoverage(Justification = "Requer Prometheus em execução")]
    private static Func<HealthCheckResult> PrometheusCheck() => () =>
        TcpCheck("comprai-prometheus", 9090, "Prometheus");

    [ExcludeFromCodeCoverage(Justification = "Requer Loki em execução")]
    private static Func<HealthCheckResult> LokiCheck() => () =>
        TcpCheck("comprai-loki", 3100, "Loki");

    [ExcludeFromCodeCoverage(Justification = "Requer Jaeger em execução")]
    private static Func<HealthCheckResult> JaegerCheck() => () =>
        TcpCheck("comprai-jaeger", 16686, "Jaeger");

    [ExcludeFromCodeCoverage(Justification = "Requer infra de rede em execução")]
    private static HealthCheckResult TcpCheck(string host, int port, string name)
    {
        try
        {
            using var tcp    = new TcpClient();
            var       result = tcp.BeginConnect(host, port, null, null);
            var       ok     = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
            if (ok && tcp.Connected) { tcp.EndConnect(result); return HealthCheckResult.Healthy(); }
            return HealthCheckResult.Degraded($"{name} porta {port} inacessível");
        }
        catch { return HealthCheckResult.Degraded($"{name} não disponível"); }
    }
}
