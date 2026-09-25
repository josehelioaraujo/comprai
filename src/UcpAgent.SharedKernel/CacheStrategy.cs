namespace UcpAgent.SharedKernel;

/// <summary>
/// Estratégia de cache chaveável via Features:CacheStrategy no appsettings / .env
/// </summary>
public enum CacheStrategy
{
    /// <summary>TTL simples — expira por tempo, sem invalidação manual.</summary>
    Ttl,

    /// <summary>Cache-Aside — aplicação controla leitura e escrita; suporta invalidação explícita.</summary>
    Aside,

    /// <summary>Read-Through — cache intercepta a leitura e busca na fonte automaticamente no miss.</summary>
    ReadThrough,

    /// <summary>
    /// Hybrid (padrão e-commerce) — Read-Through na leitura + Write-Behind na escrita + TTL curto.
    /// Miss busca na fonte e salva no cache; escrita atualiza cache imediato e fonte em background.
    /// </summary>
    Hybrid
}
