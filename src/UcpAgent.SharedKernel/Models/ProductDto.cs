namespace UcpAgent.SharedKernel.Models;

public record ProductDto(
    string Id,
    string Title,
    decimal Price,
    string? ImageUrl,
    string? Url,
    string? Category,
    string Source,
    decimal? OriginalPrice = null,
    int? AvailableQuantity = null
);
