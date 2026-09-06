namespace UcpAgent.SharedKernel.Events;

public record SearchQueryLoggedEvent(
    string Query,
    string? Category,
    decimal? MinPrice,
    decimal? MaxPrice,
    int TotalResults,
    DateTime QueriedAt);
