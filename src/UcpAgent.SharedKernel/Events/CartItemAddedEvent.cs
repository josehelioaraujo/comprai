using System.Diagnostics.CodeAnalysis;

namespace UcpAgent.SharedKernel.Events;

[ExcludeFromCodeCoverage]
public record CartItemAddedEvent(
    string SessionId,
    string ProductId,
    string ProductName,
    int    Quantity,
    decimal UnitPrice,
    DateTime AddedAt);
