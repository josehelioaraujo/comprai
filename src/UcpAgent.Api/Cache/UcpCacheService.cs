using Microsoft.Extensions.Caching.Hybrid;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Cache;

/// <summary>
/// Implementação única de ICacheService que adapta o comportamento
/// conforme a CacheStrategy configurada.
///
/// Estratégias:
///   Ttl         — GetOrCreateAsync com TTL padrão de 5min/1min. Sem invalidação.
///   Aside       — Leitura manual: tenta cache → miss → factory → SetAsync. Invalida via RemoveAsync.
///   ReadThrough — GetOrCreateAsync com TTL curto (2min). Invalida via RemoveAsync.
///   Hybrid      — Read-Through na leitura (2min L2 / 30s L1) +
///                 Write-Behind na escrita (SetAsync imediato, fonte em background via IEventPublisher).
/// </summary>
public sealed class UcpCacheService(
    HybridCache cache,
    CacheStrategy strategy) : ICacheService
{
    // TTLs por estratégia
    private static readonly TimeSpan TtlL2Default    = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TtlL1Default    = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan TtlL2Short      = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan TtlL1Short      = TimeSpan.FromSeconds(30);

    public CacheStrategy Strategy => strategy;

    public async Task<T?> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        TimeSpan? ttl = null,
        CancellationToken ct = default) where T : class
    {
        var opts = strategy switch
        {
            CacheStrategy.Hybrid or CacheStrategy.ReadThrough => new HybridCacheEntryOptions
            {
                Expiration           = ttl ?? TtlL2Short,
                LocalCacheExpiration = TtlL1Short
            },
            CacheStrategy.Aside => new HybridCacheEntryOptions
            {
                Expiration           = ttl ?? TtlL2Default,
                LocalCacheExpiration = TtlL1Default
            },
            _ => new HybridCacheEntryOptions   // Ttl
            {
                Expiration           = ttl ?? TtlL2Default,
                LocalCacheExpiration = TtlL1Default
            }
        };

        return await cache.GetOrCreateAsync(
            key,
            async token => await factory(token),
            opts,
            cancellationToken: ct);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken ct = default) where T : class
    {
        // Write-Behind: atualiza cache imediatamente
        // A atualização da fonte (plugins externos) é responsabilidade do caller em background
        var opts = new HybridCacheEntryOptions
        {
            Expiration           = ttl ?? (strategy == CacheStrategy.Hybrid ? TtlL2Short : TtlL2Default),
            LocalCacheExpiration = strategy == CacheStrategy.Hybrid ? TtlL1Short : TtlL1Default
        };

        await cache.SetAsync(key, value, opts, cancellationToken: ct);
    }

    public async Task InvalidateAsync(string key, CancellationToken ct = default)
    {
        if (strategy == CacheStrategy.Ttl)
            return; // TTL não suporta invalidação manual — expira por tempo

        await cache.RemoveAsync(key, ct);
    }
}
