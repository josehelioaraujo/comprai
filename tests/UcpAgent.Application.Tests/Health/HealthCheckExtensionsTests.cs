using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using UcpAgent.Api.Health;
using Xunit;

namespace UcpAgent.Application.Tests.Health;

public class HealthCheckExtensionsTests
{
    private static IConfiguration BuildConfig(
        string redis       = "localhost:6379",
        string ollamaBase  = "http://localhost:11434",
        string rabbitHost  = "localhost",
        string kafkaBs     = "localhost:9092")
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"]       = redis,
                ["Ollama:BaseUrl"]                = ollamaBase,
                ["RabbitMQ:Host"]                 = rabbitHost,
                ["Kafka:BootstrapServers"]         = kafkaBs,
            })
            .Build();
    }

    [Fact]
    public void AddStatusPageHealthChecks_DeveRegistrarServicos()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfig();

        services.AddStatusPageHealthChecks(config);

        var provider = services.BuildServiceProvider();
        var hcs = provider.GetService<HealthCheckService>();

        Assert.NotNull(hcs);
    }

    [Fact]
    public void AddStatusPageHealthChecks_DeveRetornarIServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfig();

        var result = services.AddStatusPageHealthChecks(config);

        Assert.NotNull(result);
        Assert.Same(services, result);
    }

    [Fact]
    public async Task HealthChecks_Redis_DeveRetornarUnhealthyQuandoIndisponivel()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfig(redis: "host-invalido:6379,connectTimeout=100,syncTimeout=100");

        services.AddStatusPageHealthChecks(config);
        var provider = services.BuildServiceProvider();
        var hcs = provider.GetRequiredService<HealthCheckService>();

        var report = await hcs.CheckHealthAsync(r => r.Tags.Contains("infra"));

        Assert.Contains("redis", report.Entries.Keys);
        Assert.NotEqual(HealthStatus.Healthy, report.Entries["redis"].Status);
    }

    [Fact]
    public async Task HealthChecks_Ollama_DeveRetornarDegradedQuandoIndisponivel()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfig(ollamaBase: "http://host-invalido-ollama:11434");

        services.AddStatusPageHealthChecks(config);
        var provider = services.BuildServiceProvider();
        var hcs = provider.GetRequiredService<HealthCheckService>();

        var report = await hcs.CheckHealthAsync(r => r.Tags.Contains("ai"));

        Assert.Contains("ollama", report.Entries.Keys);
        Assert.NotEqual(HealthStatus.Healthy, report.Entries["ollama"].Status);
    }

    [Fact]
    public async Task HealthChecks_RabbitMQ_DeveRetornarDegradedQuandoIndisponivel()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfig(rabbitHost: "host-invalido-rabbit");

        services.AddStatusPageHealthChecks(config);
        var provider = services.BuildServiceProvider();
        var hcs = provider.GetRequiredService<HealthCheckService>();

        var report = await hcs.CheckHealthAsync(r => r.Tags.Contains("messaging"));

        Assert.Contains("rabbitmq", report.Entries.Keys);
        Assert.NotEqual(HealthStatus.Healthy, report.Entries["rabbitmq"].Status);
    }

    [Fact]
    public async Task HealthChecks_Kafka_DeveRetornarDegradedQuandoIndisponivel()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfig(kafkaBs: "host-invalido-kafka:9092");

        services.AddStatusPageHealthChecks(config);
        var provider = services.BuildServiceProvider();
        var hcs = provider.GetRequiredService<HealthCheckService>();

        var report = await hcs.CheckHealthAsync(r => r.Tags.Contains("messaging"));

        Assert.Contains("kafka", report.Entries.Keys);
        Assert.NotEqual(HealthStatus.Healthy, report.Entries["kafka"].Status);
    }
}
