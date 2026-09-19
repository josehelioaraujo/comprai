using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using UcpAgent.Api.Health;
using Xunit;

namespace UcpAgent.Application.Tests.Health;

public class HealthCheckExtensionsTests
{
    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"]   = "localhost:6379",
                ["RabbitMQ:Host"]             = "localhost",
                ["Kafka:BootstrapServers"]     = "localhost:9092",
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
    public void AddStatusPageHealthChecks_DeveRegistrarTodosOsChecks()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddStatusPageHealthChecks(BuildConfig());

        // Verificar que os 5 checks foram registrados via IOptions<HealthCheckServiceOptions>
        var provider = services.BuildServiceProvider();
        var options  = provider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        var names = options.Value.Registrations.Select(r => r.Name).ToList();
        Assert.Contains("redis",         names);
        Assert.Contains("ollama",        names);
        Assert.Contains("rabbitmq",      names);
        Assert.Contains("kafka",         names);
        Assert.Contains("datadog-otel",  names);
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
    public void AddStatusPageHealthChecks_MensageriaDeveEstarNaTagMessaging()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStatusPageHealthChecks(BuildConfig());

        var provider = services.BuildServiceProvider();
        var options  = provider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<HealthCheckServiceOptions>>();

        var names = options.Value.Registrations
            .Where(r => r.Tags.Contains("messaging"))
            .Select(r => r.Name)
            .ToList();

        Assert.Contains("rabbitmq", names);
        Assert.Contains("kafka",    names);
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
