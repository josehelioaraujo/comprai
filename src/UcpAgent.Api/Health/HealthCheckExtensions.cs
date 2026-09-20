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
        services.AddHealthChecks()
            .AddCheck("redis",        RedisCheck(config),       tags: ["infra"])
            .AddCheck("ollama",       OllamaCheck(config),      tags: ["ai"])
            .AddCheck("rabbitmq",     RabbitMqCheck(config),    tags: ["messaging"])
            .AddCheck("kafka",        KafkaCheck(config),       tags: ["messaging"])
            .AddCheck("datadog-otel", DatadogCheck(),           tags: ["observability"]);

        return services;
    }

    [ExcludeFromCodeCoverage(Justification = "Requer Redis em execução")]
    private static Func<HealthCheckResult> RedisCheck(IConfiguration config) => () =>
    {
        try
        {
            var conn = config.GetConnectionString("Redis") ?? "localhost:6379";
            var mux  = ConnectionMultiplexer.Connect(conn + ",connectTimeout=1000,syncTimeout=1000");
            return mux.IsConnected
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Not connected");
        }
        catch (Exception ex) { return HealthCheckResult.Unhealthy(ex.Message); }
    };

    [ExcludeFromCodeCoverage(Justification = "Requer Ollama em execução")]
    private static Func<HealthCheckResult> OllamaCheck(IConfiguration config) => () =>
    {
        try
        {
            var baseUrl = config["Ollama__BaseUrl"] ?? config["Ollama:BaseUrl"] ?? "http://localhost:11434";
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var res = http.GetAsync($"{baseUrl.TrimEnd('/')}/api/tags").GetAwaiter().GetResult();
            return res.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded($"HTTP {(int)res.StatusCode}");
        }
        catch (Exception ex) { return HealthCheckResult.Degraded(ex.Message); }
    };

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

