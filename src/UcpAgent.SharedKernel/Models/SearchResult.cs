namespace UcpAgent.SharedKernel.Models;

public record SearchResult(
    IReadOnlyList<ProductDto> Items,
    int TotalItems,
    int Page,
    int PageSize,
    string Source
);
