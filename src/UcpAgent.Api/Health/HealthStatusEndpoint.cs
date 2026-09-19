using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UcpAgent.Api.Health;

public static class HealthStatusEndpoint
{
    private static readonly Dictionary<string, string> _componentNames = new()
    {
        ["api"]            = "API",
        ["redis"]          = "Redis Cache",
        ["ollama"]         = "Ollama (LLM)",
        ["rabbitmq"]       = "RabbitMQ",
        ["kafka"]          = "Kafka",
        ["datadog-otel"]   = "Datadog OTel Collector",
    };

    private static readonly Dictionary<string, string> _componentGroups = new()
    {
        ["api"]            = "api",
        ["redis"]          = "infra",
        ["ollama"]         = "ai",
        ["rabbitmq"]       = "messaging",
        ["kafka"]          = "messaging",
        ["datadog-otel"]   = "observability",
    };

    public static void MapHealthStatusEndpoint(this WebApplication app)
    {
        app.MapGet("/api/health/status", async (HealthCheckService healthCheckService) =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var report = await healthCheckService.CheckHealthAsync();
            sw.Stop();

            // API sempre aparece como primeiro componente (self)
            var components = new List<ComponentStatus>
            {
                new(
                    Id: "api",
                    Name: "API",
                    Group: "api",
                    Status: "operational",
                    LatencyMs: (int)sw.ElapsedMilliseconds,
                    Description: "UcpAgent.Api — Minimal API .NET 10",
                    CheckedAt: DateTime.UtcNow
                )
            };

            foreach (var (key, entry) in report.Entries)
            {
                components.Add(new ComponentStatus(
                    Id: key,
                    Name: _componentNames.GetValueOrDefault(key, key),
                    Group: _componentGroups.GetValueOrDefault(key, "infra"),
                    Status: MapStatus(entry.Status),
                    LatencyMs: (int)entry.Duration.TotalMilliseconds,
                    Description: entry.Description,
                    CheckedAt: DateTime.UtcNow
                ));
            }

            var overallIndicator = DetermineOverallIndicator(components);

            var response = new StatusPageResponse(
                Page: new StatusPageInfo(
                    Name: "Comprai Status",
                    Url: "https://comprai.2.25.122.11.nip.io",
                    UpdatedAt: DateTime.UtcNow
                ),
                Status: new StatusSummary(
                    Indicator: overallIndicator,
                    Description: GetDescription(overallIndicator)
                ),
                Components: components,
                Incidents: [] // histórico futuro via persistência
            );

            return Results.Ok(response);
        })
        .WithName("GetHealthStatus")
        .WithTags("Health")
        .AllowAnonymous();
    }

    private static string MapStatus(HealthStatus status) => status switch
    {
        HealthStatus.Healthy   => "operational",
        HealthStatus.Degraded  => "degraded",
        HealthStatus.Unhealthy => "major_outage",
        _                      => "major_outage"
    };

    private static string DetermineOverallIndicator(List<ComponentStatus> components)
    {
        if (components.Any(c => c.Status == "major_outage"))
        {
            var outageCount = components.Count(c => c.Status == "major_outage");
            return outageCount > 1 ? "major_outage" : "partial_outage";
        }
        if (components.Any(c => c.Status == "degraded")) return "degraded";
        return "operational";
    }

    private static string GetDescription(string indicator) => indicator switch
    {
        "operational"    => "All Systems Operational",
        "degraded"       => "Degraded Performance",
        "partial_outage" => "Partial System Outage",
        "major_outage"   => "Major System Outage",
        _                => "Unknown"
    };
}
