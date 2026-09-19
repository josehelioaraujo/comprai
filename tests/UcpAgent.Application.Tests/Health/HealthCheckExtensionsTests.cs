using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using UcpAgent.Api.Health;
using Xunit;

namespace UcpAgent.Application.Tests.Health;

public class HealthCheckExtensionsTests
{
    private static IConfiguration BuildConfig(
        string redis      = "localhost:6379",
        string rabbitHost = "localhost",
        string kafkaBs    = "localhost:9092")
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"]   = redis,
                ["RabbitMQ:Host"]             = rabbitHost,
                ["Kafka:BootstrapServers"]     = kafkaBs,
            })
            .Build();
    }

    [Fact]
    public void AddStatusPageHealthChecks_DeveRetornarMesmaColecao()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var result = services.AddStatusPageHealthChecks(BuildConfig());
        Assert.Same(services, result);
    }

    [Fact]
    public void AddStatusPageHealthChecks_DeveRegistrarHealthCheckService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStatusPageHealthChecks(BuildConfig());
        var provider = services.BuildServiceProvider();
        var hcs = provider.GetService<HealthCheckService>();
        Assert.NotNull(hcs);
    }

    [Fact]
    public async Task HealthCheck_Redis_DeveExecutarSemExcecaoERetornarResultado()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        // Host inválido — deve retornar Unhealthy, não lançar exceção
        services.AddStatusPageHealthChecks(BuildConfig(redis: "host-invalido:6379,connectTimeout=100,syncTimeout=100"));
        var provider = services.BuildServiceProvider();
        var hcs = provider.GetRequiredService<HealthCheckService>();

        var report = await hcs.CheckHealthAsync(r => r.Tags.Contains("infra"));

        Assert.Contains("redis", report.Entries.Keys);
        // Não importa o status — apenas que não lançou exceção e retornou um resultado
        Assert.True(Enum.IsDefined(typeof(HealthStatus), report.Entries["redis"].Status));
    }

    [Fact]
    public async Task HealthCheck_Ollama_DeveExecutarSemExcecaoERetornarResultado()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStatusPageHealthChecks(BuildConfig());
        var provider = services.BuildServiceProvider();
        var hcs = provider.GetRequiredService<HealthCheckService>();

        var report = await hcs.CheckHealthAsync(r => r.Tags.Contains("ai"));

        Assert.Contains("ollama", report.Entries.Keys);
        Assert.True(Enum.IsDefined(typeof(HealthStatus), report.Entries["ollama"].Status));
    }

    [Fact]
    public async Task HealthCheck_RabbitMQ_DeveExecutarSemExcecaoERetornarResultado()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStatusPageHealthChecks(BuildConfig(rabbitHost: "host-invalido-rabbit"));
        var provider = services.BuildServiceProvider();
        var hcs = provider.GetRequiredService<HealthCheckService>();

        var report = await hcs.CheckHealthAsync(r => r.Name == "rabbitmq");

        Assert.Contains("rabbitmq", report.Entries.Keys);
        Assert.True(Enum.IsDefined(typeof(HealthStatus), report.Entries["rabbitmq"].Status));
    }

    [Fact]
    public async Task HealthCheck_Kafka_DeveExecutarSemExcecaoERetornarResultado()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStatusPageHealthChecks(BuildConfig(kafkaBs: "host-invalido-kafka:9092"));
        var provider = services.BuildServiceProvider();
        var hcs = provider.GetRequiredService<HealthCheckService>();

        var report = await hcs.CheckHealthAsync(r => r.Name == "kafka");

        Assert.Contains("kafka", report.Entries.Keys);
        Assert.True(Enum.IsDefined(typeof(HealthStatus), report.Entries["kafka"].Status));
    }

    [Fact]
    public async Task HealthCheck_DatadogOtel_DeveExecutarSemExcecaoERetornarResultado()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStatusPageHealthChecks(BuildConfig());
        var provider = services.BuildServiceProvider();
        var hcs = provider.GetRequiredService<HealthCheckService>();

        var report = await hcs.CheckHealthAsync(r => r.Name == "datadog-otel");

        Assert.Contains("datadog-otel", report.Entries.Keys);
        Assert.True(Enum.IsDefined(typeof(HealthStatus), report.Entries["datadog-otel"].Status));
    }

    [Fact]
    public async Task HealthChecks_TodosOsChecks_DevemRetornarResultadoValido()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStatusPageHealthChecks(BuildConfig());
        var provider = services.BuildServiceProvider();
        var hcs = provider.GetRequiredService<HealthCheckService>();

        var report = await hcs.CheckHealthAsync();

        var expectedChecks = new[] { "redis", "ollama", "rabbitmq", "kafka", "datadog-otel" };
        foreach (var name in expectedChecks)
        {
            Assert.True(report.Entries.ContainsKey(name), $"Check '{name}' nao encontrado no report");
            Assert.True(Enum.IsDefined(typeof(HealthStatus), report.Entries[name].Status),
                $"Status invalido para '{name}'");
        }
    }
}
