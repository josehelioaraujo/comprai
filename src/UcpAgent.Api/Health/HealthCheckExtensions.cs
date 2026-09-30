// V041 — health checks reais | build ghcr.io, VPS só pull
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
        var usarKafka    = config.GetValue<bool>("Features:UsarKafka");
        var usarRabbitMQ = config.GetValue<bool>("Features:UsarRabbitMQ");

        var hc = services.AddHealthChecks()
            .AddCheck<RedisHealthCheck> ("redis",        tags: ["infra"])
            .AddCheck<OllamaHealthCheck>("ollama",       tags: ["ai"])
            .AddCheck("datadog-otel", DatadogCheck(),   tags: ["observability"])
            .AddCheck("prometheus",   PrometheusCheck(), tags: ["observability"])
            .AddCheck("loki",         LokiCheck(),       tags: ["observability"])
            .AddCheck("jaeger",       JaegerCheck(),     tags: ["observability"]);

        // Só checa o broker que está ativo pela feature flag
        if (usarRabbitMQ)
            hc.AddCheck("rabbitmq", RabbitMqCheck(config), tags: ["messaging"]);

        if (usarKafka)
            hc.AddCheck("kafka", KafkaCheck(config), tags: ["messaging"]);

        return services;
    }

    // ── Redis: resolve via IServiceProvider (opcional — sem exception sem Redis) ─
    public class RedisHealthCheck(IServiceProvider sp) : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
        {
            var mux = sp.GetService<IConnectionMultiplexer>();
            if (mux is null)
                return Task.FromResult(HealthCheckResult.Degraded("Redis não configurado"));
            return Task.FromResult(mux.IsConnected
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Not connected"));
        }
    }

    // ── Ollama: async real, timeout 2s ────────────────────────────────────────
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

    // ── TCP check com timeout 500ms ───────────────────────────────────────────
    [ExcludeFromCodeCoverage(Justification = "Requer infra de rede em execução")]
    private static HealthCheckResult TcpCheck(string host, int port, string name)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            using var tcp = new TcpClient();
            var task = tcp.ConnectAsync(host, port, cts.Token).AsTask();
            task.Wait(cts.Token);
            return tcp.Connected
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded($"{name} porta {port} inacessível");
        }
        catch (OperationCanceledException) { return HealthCheckResult.Degraded($"{name} timeout"); }
        catch { return HealthCheckResult.Degraded($"{name} não disponível"); }
    }
}
