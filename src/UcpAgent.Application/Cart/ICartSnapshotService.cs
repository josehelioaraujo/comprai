using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Cart;

public interface ICartSnapshotService
{
    /// <summary>Salva o snapshot após add/remove — diff interno evita writes desnecessários.</summary>
    Task PersistAsync(string sessionId, ICartPort cart, CancellationToken ct = default);

    /// <summary>Remove o snapshot após checkout concluído.</summary>
    Task DeleteAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Recupera o snapshot; null se não existir.</summary>
    Task<CartSnapshotDto?> GetAsync(string sessionId, CancellationToken ct = default);
}
