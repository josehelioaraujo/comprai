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
            .AddCheck("redis", () =>
            {
                try
                {
                    var conn = config.GetConnectionString("Redis") ?? "localhost:6379";
                    var mux = ConnectionMultiplexer.Connect(conn + ",connectTimeout=1000,syncTimeout=1000");
                    return mux.IsConnected
                        ? HealthCheckResult.Healthy()
                        : HealthCheckResult.Unhealthy("Not connected");
                }
                catch (Exception ex) { return HealthCheckResult.Unhealthy(ex.Message); }
            }, tags: ["infra"])

            .AddCheck("ollama", () =>
            {
                try
                {
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                    var res = http.GetAsync("http://localhost:11434/api/tags").GetAwaiter().GetResult();
                    return res.IsSuccessStatusCode
                        ? HealthCheckResult.Healthy()
                        : HealthCheckResult.Degraded($"HTTP {(int)res.StatusCode}");
                }
                catch (Exception ex) { return HealthCheckResult.Degraded(ex.Message); }
            }, tags: ["ai"])

            .AddCheck("rabbitmq", () => TcpCheck(
                config["RabbitMQ:Host"] ?? "localhost", 5672, "RabbitMQ"), tags: ["messaging"])

            .AddCheck("kafka", () => TcpCheck(
                (config["Kafka:BootstrapServers"] ?? "localhost:9092").Split(':')[0],
                int.TryParse((config["Kafka:BootstrapServers"] ?? "localhost:9092").Split(':').ElementAtOrDefault(1), out var p) ? p : 9092,
                "Kafka"), tags: ["messaging"])

            .AddCheck("datadog-otel", () => TcpCheck(
                "comprai-otel-collector", 4317, "OTel Collector"), tags: ["observability"]);

        return services;
    }

    private static HealthCheckResult TcpCheck(string host, int port, string name)
    {
        try
        {
            using var tcp = new TcpClient();
            var result = tcp.BeginConnect(host, port, null, null);
            var ok = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
            if (ok && tcp.Connected) { tcp.EndConnect(result); return HealthCheckResult.Healthy(); }
            return HealthCheckResult.Degraded($"{name} porta {port} inacessível");
        }
        catch { return HealthCheckResult.Degraded($"{name} não disponível"); }
    }
}
