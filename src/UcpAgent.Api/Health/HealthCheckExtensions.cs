using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace UcpAgent.Api.Health;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddStatusPageHealthChecks(
        this IServiceCollection services,
        IConfiguration config)
    {
        var builder = services.AddHealthChecks();

        // Redis
        var redisConn = config.GetConnectionString("Redis") ?? "localhost:6379";
        builder.AddCheck("redis", () =>
        {
            try
            {
                var mux = ConnectionMultiplexer.Connect(redisConn + ",connectTimeout=1000,syncTimeout=1000");
                return mux.IsConnected
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy("Not connected");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(ex.Message);
            }
        }, tags: ["infra"]);

        // Ollama
        builder.AddCheck("ollama", () =>
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var res = http.GetAsync("http://localhost:11434/api/tags").GetAwaiter().GetResult();
                return res.IsSuccessStatusCode
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Degraded($"HTTP {(int)res.StatusCode}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(ex.Message);
            }
        }, tags: ["ai"]);

        // RabbitMQ
        builder.AddCheck("rabbitmq", () =>
        {
            try
            {
                var host = config["RabbitMQ:Host"] ?? "localhost";
                var factory = new RabbitMQ.Client.ConnectionFactory { HostName = host, RequestedConnectionTimeout = TimeSpan.FromSeconds(2) };
                using var conn = factory.CreateConnection();
                return conn.IsOpen
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy("Connection closed");
            }
            catch
            {
                return HealthCheckResult.Degraded("Unavailable (feature flag off or not running)");
            }
        }, tags: ["messaging"]);

        // Kafka
        builder.AddCheck("kafka", () =>
        {
            try
            {
                var brokers = config["Kafka:BootstrapServers"] ?? "localhost:9092";
                using var admin = new Confluent.Kafka.AdminClientBuilder(
                    new Confluent.Kafka.AdminClientConfig { BootstrapServers = brokers, SocketTimeoutMs = 2000 })
                    .Build();
                var meta = admin.GetMetadata(TimeSpan.FromSeconds(2));
                return meta.Brokers.Count > 0
                    ? HealthCheckResult.Healthy($"{meta.Brokers.Count} broker(s)")
                    : HealthCheckResult.Unhealthy("No brokers");
            }
            catch
            {
                return HealthCheckResult.Degraded("Unavailable (feature flag off or not running)");
            }
        }, tags: ["messaging"]);

        // Datadog OTel Collector
        builder.AddCheck("datadog-otel", () =>
        {
            try
            {
                using var tcp = new System.Net.Sockets.TcpClient();
                var result = tcp.BeginConnect("comprai-otel-collector", 4317, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                return success && tcp.Connected
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Degraded("Port 4317 not reachable");
            }
            catch
            {
                return HealthCheckResult.Degraded("Not reachable");
            }
        }, tags: ["observability"]);

        return services;
    }
}
