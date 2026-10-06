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
    /// Retorna o UUID persistido (pode ser diferente se o clientId não for UUID válido).
    /// </summary>
    Task<Guid> GetOrCreateAsync(string? clientSessionId, string channel = "web",
        CancellationToken ct = default);
}
