using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace UcpAgent.Application.Tests.Health;

public class HealthTestFactory : WebApplicationFactory<Program>
{
    public HealthStatus OllamaStatus  { get; set; } = HealthStatus.Degraded;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureServices(services =>
        {
            services.AddHealthChecks()
                .AddCheck("redis",        () => HealthCheckResult.Healthy(),               tags: ["infra"])
                .AddCheck("ollama",       () => StatusToResult(OllamaStatus),              tags: ["ai"])
                .AddCheck("rabbitmq",     () => HealthCheckResult.Degraded("mock"),        tags: ["messaging"])
                .AddCheck("kafka",        () => HealthCheckResult.Degraded("mock"),        tags: ["messaging"])
                .AddCheck("datadog-otel", () => HealthCheckResult.Healthy(),               tags: ["observability"]);
        });
    }

    private static HealthCheckResult StatusToResult(HealthStatus s) => s switch
    {
        HealthStatus.Healthy   => HealthCheckResult.Healthy(),
        HealthStatus.Degraded  => HealthCheckResult.Degraded("mock degraded"),
        _                      => HealthCheckResult.Unhealthy("mock unhealthy")
    };
}

public class HealthStatusEndpointTests : IClassFixture<HealthTestFactory>
{
    private readonly HttpClient _client;

    public HealthStatusEndpointTests(HealthTestFactory factory)
    {
        _client = factory.CreateClient();
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

    [Fact]
    public async Task GetHealthStatus_TodosComponentesDevemTerStatusValido()
    {
        var valid = new[] { "operational", "degraded", "partial_outage", "major_outage" };
        var response = await _client.GetAsync("/api/health/status");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        foreach (var comp in doc.RootElement.GetProperty("components").EnumerateArray())
            Assert.Contains(comp.GetProperty("status").GetString(), valid);
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
    public async Task GetHealthStatus_StatusIndicator_ComDegradados_NaoDeveSerOperational()
    {
        var response = await _client.GetAsync("/api/health/status");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var indicator = doc.RootElement.GetProperty("status").GetProperty("indicator").GetString();

        Assert.NotEqual("operational", indicator);
    }
}
