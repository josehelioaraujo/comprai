namespace UcpAgent.SharedKernel.Ports;

public sealed record SessionInfo(
    Guid     Id,
    string   Channel,
    DateTime CreatedAt,
    DateTime ExpiresAt);

public interface ISessionPort
{
    /// <summary>
    /// Obtém ou cria uma session no BD a partir do sessionId do cliente (localStorage UUID).
    /// </summary>
    Task<Guid> GetOrCreateAsync(string? clientSessionId, string channel = "web",
        CancellationToken ct = default);

    /// <summary>
    /// Vincula customer_id à session após login/registro. Fire-and-forget safe.
    /// </summary>
    Task LinkCustomerAsync(Guid sessionId, Guid customerId,
        CancellationToken ct = default);
}
