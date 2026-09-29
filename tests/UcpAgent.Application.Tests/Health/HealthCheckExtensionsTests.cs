using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using UcpAgent.Api.Health;
using Xunit;

namespace UcpAgent.Application.Tests.Health;

public class HealthCheckExtensionsTests
{
    // Config base — sem flags de mensageria ativas
    private static IConfiguration BuildConfig(bool usarKafka = false, bool usarRabbitMQ = false) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"]    = "localhost:6379",
                ["RabbitMQ:Host"]              = "localhost",
                ["Kafka:BootstrapServers"]     = "localhost:9092",
                ["Features:UsarKafka"]         = usarKafka.ToString().ToLower(),
                ["Features:UsarRabbitMQ"]      = usarRabbitMQ.ToString().ToLower(),
            })
            .Build();

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
        Assert.NotNull(provider.GetService<HealthCheckService>());
    }

    [Fact]
    public void AddStatusPageHealthChecks_DeveRegistrarChecksBase()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStatusPageHealthChecks(BuildConfig());

        var provider = services.BuildServiceProvider();
        var options  = provider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        var names = options.Value.Registrations.Select(r => r.Name).ToList();

        // Checks sempre presentes (independente de feature flags)
        Assert.Contains("redis",        names);
        Assert.Contains("ollama",       names);
        Assert.Contains("datadog-otel", names);
        Assert.Contains("prometheus",   names);
        Assert.Contains("loki",         names);
        Assert.Contains("jaeger",       names);
    }

    [Fact]
    public void AddStatusPageHealthChecks_KafkaNaoRegistradoSeFlagDesligada()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStatusPageHealthChecks(BuildConfig(usarKafka: false, usarRabbitMQ: false));

        var provider = services.BuildServiceProvider();
        var options  = provider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        var names = options.Value.Registrations.Select(r => r.Name).ToList();
        Assert.DoesNotContain("kafka",    names);
        Assert.DoesNotContain("rabbitmq", names);
    }

    [Fact]
    public void AddStatusPageHealthChecks_KafkaRegistradoSeFlagAtiva()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStatusPageHealthChecks(BuildConfig(usarKafka: true));

        var provider = services.BuildServiceProvider();
        var options  = provider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        var names = options.Value.Registrations.Select(r => r.Name).ToList();
        Assert.Contains("kafka", names);
        Assert.DoesNotContain("rabbitmq", names);
    }

    [Fact]
    public void AddStatusPageHealthChecks_RabbitMQRegistradoSeFlagAtiva()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStatusPageHealthChecks(BuildConfig(usarRabbitMQ: true));

        var provider = services.BuildServiceProvider();
        var options  = provider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        var names = options.Value.Registrations.Select(r => r.Name).ToList();
        Assert.Contains("rabbitmq", names);
        Assert.DoesNotContain("kafka", names);
    }

    [Fact]
    public void AddStatusPageHealthChecks_RedisDeveEstarNaTagInfra()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStatusPageHealthChecks(BuildConfig());

        var provider = services.BuildServiceProvider();
        var options  = provider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        var redis = options.Value.Registrations.FirstOrDefault(r => r.Name == "redis");
        Assert.NotNull(redis);
        Assert.Contains("infra", redis!.Tags);
    }

    [Fact]
    public void AddStatusPageHealthChecks_OllamaDeveEstarNaTagAi()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStatusPageHealthChecks(BuildConfig());

        var provider = services.BuildServiceProvider();
        var options  = provider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        var ollama = options.Value.Registrations.FirstOrDefault(r => r.Name == "ollama");
        Assert.NotNull(ollama);
        Assert.Contains("ai", ollama!.Tags);
    }

    [Fact]
    public void AddStatusPageHealthChecks_KafkaDeveEstarNaTagMessagingQuandoAtivo()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStatusPageHealthChecks(BuildConfig(usarKafka: true));

        var provider = services.BuildServiceProvider();
        var options  = provider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        var kafka = options.Value.Registrations.FirstOrDefault(r => r.Name == "kafka");
        Assert.NotNull(kafka);
        Assert.Contains("messaging", kafka!.Tags);
    }

    [Fact]
    public void AddStatusPageHealthChecks_DatadogDeveEstarNaTagObservability()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStatusPageHealthChecks(BuildConfig());

        var provider = services.BuildServiceProvider();
        var options  = provider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        var datadog = options.Value.Registrations.FirstOrDefault(r => r.Name == "datadog-otel");
        Assert.NotNull(datadog);
        Assert.Contains("observability", datadog!.Tags);
    }
}
