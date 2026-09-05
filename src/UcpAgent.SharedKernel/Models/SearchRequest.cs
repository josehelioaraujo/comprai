namespace UcpAgent.SharedKernel.Models;

public record SearchRequest(
    string Query,
    int Page = 1,
    int PageSize = 20,
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null
);
