namespace UcpAgent.Api.Models;

public sealed record IntentRequest(
    string  Text,
    string? SessionId = null
);
