using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace UcpAgent.Application.Tests.Health;

// Factory leve para testes de health — substitui todos os checks por healthy/unhealthy mock
public class HealthTestFactory : WebApplicationFactory<Program>
{
    public HealthStatus RedisStatus   { get; set; } = HealthStatus.Healthy;
    public HealthStatus OllamaStatus  { get; set; } = HealthStatus.Degraded;
    public HealthStatus RabbitStatus  { get; set; } = HealthStatus.Degraded;
    public HealthStatus KafkaStatus   { get; set; } = HealthStatus.Degraded;
    public HealthStatus DatadogStatus { get; set; } = HealthStatus.Healthy;

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureServices(services =>
        {
            // Remover todos os health checks registrados e substituir por mocks
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(HealthCheckService));

            // Registrar checks mock com status controlado pelo teste
            services.AddHealthChecks()
                .AddCheck("redis",        () => StatusToResult(RedisStatus),   tags: ["infra"])
                .AddCheck("ollama",       () => StatusToResult(OllamaStatus),  tags: ["ai"])
                .AddCheck("rabbitmq",     () => StatusToResult(RabbitStatus),  tags: ["messaging"])
                .AddCheck("kafka",        () => StatusToResult(KafkaStatus),   tags: ["messaging"])
                .AddCheck("datadog-otel", () => StatusToResult(DatadogStatus), tags: ["observability"]);
        });
    }

    private static HealthCheckResult StatusToResult(HealthStatus status) => status switch
    {
        HealthStatus.Healthy   => HealthCheckResult.Healthy(),
        HealthStatus.Degraded  => HealthCheckResult.Degraded("mock degraded"),
        HealthStatus.Unhealthy => HealthCheckResult.Unhealthy("mock unhealthy"),
        _                      => HealthCheckResult.Unhealthy("unknown")
    };
}

public class HealthStatusEndpointTests : IClassFixture<HealthTestFactory>
{
    private readonly HttpClient _client;
    private readonly HealthTestFactory _factory;

    public HealthStatusEndpointTests(HealthTestFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealthStatus_DeveRetornarOk()
    {
        var response = await _client.GetAsync("/api/health/status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealthStatus_DeveRetornarJsonValido()
    {
        var response = await _client.GetAsync("/api/health/status");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("page",       out _));
        Assert.True(doc.RootElement.TryGetProperty("status",     out _));
        Assert.True(doc.RootElement.TryGetProperty("components", out _));
        Assert.True(doc.RootElement.TryGetProperty("incidents",  out _));
    }

    [Fact]
    public async Task GetHealthStatus_PageDeveTerNomeEUrl()
    {
        var response = await _client.GetAsync("/api/health/status");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var page = doc.RootElement.GetProperty("page");

        Assert.False(string.IsNullOrEmpty(page.GetProperty("name").GetString()));
        Assert.False(string.IsNullOrEmpty(page.GetProperty("url").GetString()));
    }

    [Fact]
    public async Task GetHealthStatus_ComponentsDeveConterApi()
    {
        var response = await _client.GetAsync("/api/health/status");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var comps = doc.RootElement.GetProperty("components").EnumerateArray().ToList();

        Assert.NotEmpty(comps);
        Assert.Contains(comps, c => c.GetProperty("id").GetString() == "api");
    }

    [Fact]
    public async Task GetHealthStatus_StatusIndicator_QuandoHaDegradados_NaoDeveSerOperational()
    {
        // Factory tem Ollama=Degraded por default
        var response = await _client.GetAsync("/api/health/status");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var indicator = doc.RootElement.GetProperty("status").GetProperty("indicator").GetString();

        Assert.NotEqual("operational", indicator);
    }

    [Fact]
    public async Task GetHealthStatus_ComponentsDeveConterRedis()
    {
        var response = await _client.GetAsync("/api/health/status");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var comps = doc.RootElement.GetProperty("components").EnumerateArray().ToList();

        Assert.Contains(comps, c => c.GetProperty("id").GetString() == "redis");
    }

    [Fact]
    public async Task GetHealthStatus_TodosComponentesDevemTerStatusValido()
    {
        var validStatuses = new[] { "operational", "degraded", "partial_outage", "major_outage" };
        var response = await _client.GetAsync("/api/health/status");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var comps = doc.RootElement.GetProperty("components").EnumerateArray();

        foreach (var comp in comps)
        {
            var status = comp.GetProperty("status").GetString();
            Assert.Contains(status, validStatuses);
        }
    }

    [Fact]
    public async Task GetHealthStatus_IncidentesDeveSerArray()
    {
        var response = await _client.GetAsync("/api/health/status");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("incidents").ValueKind);
    }

    [Fact]
    public async Task GetHealthStatus_ComponenteApiDeveSerOperational()
    {
        var response = await _client.GetAsync("/api/health/status");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var api = doc.RootElement.GetProperty("components").EnumerateArray()
            .FirstOrDefault(c => c.GetProperty("id").GetString() == "api");

        Assert.NotEqual(default, api);
        Assert.Equal("operational", api.GetProperty("status").GetString());
    }
}
