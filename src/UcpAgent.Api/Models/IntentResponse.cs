namespace UcpAgent.Api.Models;

public sealed record IntentResponse(
    string  Intent,
    bool    Success,
    object? Data,
    string? Message = null
);
