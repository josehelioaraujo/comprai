using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace UcpAgent.Application.Tests.Health;

// Usa a CompraApiFactory existente — sem registrar checks duplicados
public class HealthStatusEndpointTests : IClassFixture<CompraApiFactory>
{
    private readonly HttpClient _client;

    public HealthStatusEndpointTests(CompraApiFactory factory)
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
    public async Task GetHealthStatus_PageDeveTerNome()
    {
        var response = await _client.GetAsync("/api/health/status");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.False(string.IsNullOrEmpty(
            doc.RootElement.GetProperty("page").GetProperty("name").GetString()));
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
    public async Task GetHealthStatus_TodosStatusDevemSerValidos()
    {
        var valid    = new[] { "operational", "degraded", "partial_outage", "major_outage" };
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

        Assert.Equal(JsonValueKind.Array,
            doc.RootElement.GetProperty("incidents").ValueKind);
    }

    [Fact]
    public async Task GetHealthStatus_StatusIndicatorDeveSerValido()
    {
        var valid    = new[] { "operational", "degraded", "partial_outage", "major_outage" };
        var response = await _client.GetAsync("/api/health/status");
        var json     = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.Contains(
            doc.RootElement.GetProperty("status").GetProperty("indicator").GetString(),
            valid);
    }
}
