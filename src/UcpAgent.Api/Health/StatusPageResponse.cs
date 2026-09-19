namespace UcpAgent.Api.Health;

// Schema agnóstico padrão indústria (Atlassian Statuspage / Instatus compatível)
// Quando StatusForge existir, Comprai vira cliente sem refatoração

public record StatusPageResponse(
    StatusPageInfo Page,
    StatusSummary Status,
    List<ComponentStatus> Components,
    List<Incident> Incidents
);

public record StatusPageInfo(
    string Name,
    string Url,
    DateTime UpdatedAt
);

public record StatusSummary(
    string Indicator,   // operational | degraded | partial_outage | major_outage
    string Description
);

public record ComponentStatus(
    string Id,
    string Name,
    string Group,       // infra | messaging | ai | observability | api
    string Status,      // operational | degraded | partial_outage | major_outage
    int? LatencyMs,
    string? Description,
    DateTime CheckedAt
);

public record Incident(
    string Id,
    string Name,
    string Status,      // investigating | identified | monitoring | resolved
    string Impact,      // none | minor | major | critical
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    List<IncidentUpdate> Updates
);

public record IncidentUpdate(
    string Status,
    string Body,
    DateTime CreatedAt
);
