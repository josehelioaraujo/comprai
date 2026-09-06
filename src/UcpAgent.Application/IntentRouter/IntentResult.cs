namespace UcpAgent.Application.IntentRouter;

public sealed record IntentResult(
    IntentType Intent,
    string?    ExtractedQuery,
    string?    ProductId,
    string?    SessionId,
    string?    OrderId,
    string     RawInput
);
