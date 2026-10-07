namespace UcpAgent.SharedKernel.Ports;

/// <summary>
/// Serviço de snapshot do carrinho — lógica de negócio acima do ICartSnapshotPort.
/// Centraliza save/delete/get com diff, tratamento de erros e extensibilidade futura.
/// </summary>
public interface ICartSnapshotService
{
    /// <summary>Salva o snapshot após add/remove — chama GetItemsAsync internamente.</summary>
    Task PersistAsync(string sessionId, ICartPort cart, CancellationToken ct = default);

    /// <summary>Remove o snapshot após checkout concluído.</summary>
    Task DeleteAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Recupera o snapshot; null se não existir.</summary>
    Task<CartSnapshotDto?> GetAsync(string sessionId, CancellationToken ct = default);
}
