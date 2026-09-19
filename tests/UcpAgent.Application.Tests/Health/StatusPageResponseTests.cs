using UcpAgent.Api.Health;
using Xunit;

namespace UcpAgent.Application.Tests.Health;

public class StatusPageResponseTests
{
    [Fact]
    public void StatusPageResponse_DeveCriarComValoresCorretos()
    {
        var page = new StatusPageInfo("Comprai Status", "https://comprai.example.com", DateTime.UtcNow);
        var status = new StatusSummary("operational", "All Systems Operational");
        var components = new List<ComponentStatus>
        {
            new("api", "API", "api", "operational", 12, null, DateTime.UtcNow)
        };
        var incidents = new List<Incident>();

        var response = new StatusPageResponse(page, status, components, incidents);

        Assert.Equal("Comprai Status", response.Page.Name);
        Assert.Equal("operational", response.Status.Indicator);
        Assert.Single(response.Components);
        Assert.Empty(response.Incidents);
    }

    [Theory]
    [InlineData("operational",    "All Systems Operational")]
    [InlineData("degraded",       "Degraded Performance")]
    [InlineData("partial_outage", "Partial System Outage")]
    [InlineData("major_outage",   "Major System Outage")]
    public void StatusSummary_DeveAceitarTodosOsIndicadores(string indicator, string description)
    {
        var status = new StatusSummary(indicator, description);

        Assert.Equal(indicator,   status.Indicator);
        Assert.Equal(description, status.Description);
    }

    [Fact]
    public void ComponentStatus_DeveCriarComLatencia()
    {
        var comp = new ComponentStatus("redis", "Redis Cache", "infra", "major_outage", 165, "Connection refused", DateTime.UtcNow);

        Assert.Equal("redis",        comp.Id);
        Assert.Equal("Redis Cache",  comp.Name);
        Assert.Equal("infra",        comp.Group);
        Assert.Equal("major_outage", comp.Status);
        Assert.Equal(165,            comp.LatencyMs);
    }

    [Fact]
    public void ComponentStatus_DeveAceitarLatenciaNula()
    {
        var comp = new ComponentStatus("ollama", "Ollama", "ai", "degraded", null, null, DateTime.UtcNow);

        Assert.Null(comp.LatencyMs);
    }

    [Fact]
    public void Incident_DeveCriarComUpdates()
    {
        var update = new IncidentUpdate("investigating", "Investigando a causa raiz.", DateTime.UtcNow);
        var incident = new Incident(
            "inc-001", "Redis Outage", "investigating", "major",
            DateTime.UtcNow, null,
            new List<IncidentUpdate> { update }
        );

        Assert.Equal("inc-001",        incident.Id);
        Assert.Equal("investigating",  incident.Status);
        Assert.Single(incident.Updates);
        Assert.Null(incident.ResolvedAt);
    }

    [Fact]
    public void StatusPageInfo_DeveTerUrlETimestamp()
    {
        var now = DateTime.UtcNow;
        var info = new StatusPageInfo("Test", "https://test.com", now);

        Assert.Equal("Test",          info.Name);
        Assert.Equal("https://test.com", info.Url);
        Assert.Equal(now,             info.UpdatedAt);
    }
}
