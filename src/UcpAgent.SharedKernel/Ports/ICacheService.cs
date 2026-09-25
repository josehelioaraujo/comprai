namespace UcpAgent.SharedKernel.Ports;

/// <summary>
/// Porta de cache agnóstica à estratégia.
/// Configure via Features:CacheStrategy = "ttl" | "aside" | "read-through" | "hybrid"
/// </summary>
public interface ICacheService
{
    CacheStrategy Strategy { get; }

    /// <summary>
    /// Lê do cache. Se não existir (miss), executa a factory, armazena e retorna.
    /// </summary>
    Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? ttl = null,
        CancellationToken ct = default) where T : class;

    /// <summary>
    /// Escrita direta no cache (Write-Behind: atualiza cache agora, fonte em background).
    /// </summary>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken ct = default) where T : class;

    /// <summary>
    /// Invalida uma chave do cache manualmente.
    /// </summary>
    Task InvalidateAsync(string key, CancellationToken ct = default);
}
