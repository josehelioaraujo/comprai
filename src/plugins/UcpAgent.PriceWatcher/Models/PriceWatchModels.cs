namespace UcpAgent.PriceWatcher.Models;

public record PriceWatchRequest(
    string ProductId,
    string Title,
    decimal CurrentPrice,
    decimal TargetPrice,
    string? ImageUrl,
    string? Email
);

public record PriceWatch(
    string WatchId,
    string SessionId,
    string ProductId,
    string Title,
    decimal CurrentPrice,
    decimal TargetPrice,
    string? ImageUrl,
    string? Email,
    DateTime CreatedAt
);

public record PriceAlertMessage(
    string WatchId,
    string SessionId,
    string ProductId,
    string Title,
    decimal OldPrice,
    decimal NewPrice,
    decimal TargetPrice,
    string? ImageUrl,
    DateTime AlertedAt
);
