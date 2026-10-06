namespace UcpAgent.SharedKernel.Ports;

public sealed record OrderHistoryEntry(
    string   OrderId,
    Guid?    CustomerId,
    string   Status,          // delivered | cancelled | returned
    decimal  TotalAmount,
    string   ItemsSnapshot);  // JSON dos itens no momento final

public interface IOrderHistoryPort
{
    Task InsertAsync(OrderHistoryEntry entry, CancellationToken ct = default);
}
