namespace UcpAgent.Application.Cart;

/// <summary>
/// Serviço de snapshot do carrinho — lógica de negócio acima do port de infraestrutura.
/// Responsável por orquestrar save/delete/get com diff, tratamento de erros e extensibilidade futura
/// (TTL, retry, revalidação de preços, remarketing de carrinho abandonado).
/// </summary>
public interface ICartSnapshotService
{
    /// <summary>Salva o snapshot após add/remove — diff interno evita writes desnecessários.</summary>
    Task PersistAsync(string sessionId, ICartPort cart, CancellationToken ct = default);

    /// <summary>Remove o snapshot após checkout concluído.</summary>
    Task DeleteAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Recupera o snapshot; null se não existir.</summary>
    Task<UcpAgent.SharedKernel.Ports.CartSnapshotDto?> GetAsync(string sessionId, CancellationToken ct = default);
}
