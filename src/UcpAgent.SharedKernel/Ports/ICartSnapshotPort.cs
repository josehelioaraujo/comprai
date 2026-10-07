namespace UcpAgent.SharedKernel.Ports;

public sealed record CartSnapshotDto(
    string                    SessionId,
    IReadOnlyList<CartItemDto> Items,
    decimal                   TotalAmount,
    DateTime                  UpdatedAt);

public interface ICartSnapshotPort
{
    /// <summary>Upsert do snapshot do carrinho no BD (fire-and-forget safe).</summary>
    Task SaveAsync(string sessionId, IReadOnlyList<CartItemDto> items, CancellationToken ct = default);

    /// <summary>Recupera o snapshot salvo; null se não existir ou expirado.</summary>
    Task<CartSnapshotDto?> GetAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Remove o snapshot após checkout concluído.</summary>
    Task DeleteAsync(string sessionId, CancellationToken ct = default);
}
