using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Diagnostics.CodeAnalysis;

namespace UcpAgent.Api;

/// <summary>
/// Instrumentos OTel centralizados para o Funil UCP, Plugins, Cache Redis e LLM/Ollama.
/// Registrado como Singleton — injetado nos handlers via DI.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UcpMetrics : IDisposable
{
    public const string MeterName = "comprai.ucp";

    private readonly Meter _meter;

    // ── Funil UCP ────────────────────────────────────────────────────────────
    /// <summary>Buscas iniciadas no catálogo (fan-out paralelo)</summary>
    public Counter<long> SearchTotal { get; }

    /// <summary>Itens retornados por busca (tag: source = plugin name)</summary>
    public Histogram<double> SearchResultsCount { get; }

    /// <summary>Latência da busca agregada (ms)</summary>
    public Histogram<double> SearchDurationMs { get; }

    /// <summary>Produtos adicionados ao carrinho</summary>
    public Counter<long> CartAddTotal { get; }

    /// <summary>Checkouts iniciados</summary>
    public Counter<long> CheckoutTotal { get; }

    /// <summary>Checkouts concluídos com sucesso</summary>
    public Counter<long> CheckoutSuccessTotal { get; }

    /// <summary>Checkouts com falha</summary>
    public Counter<long> CheckoutFailureTotal { get; }

    /// <summary>Pedidos criados (order placed)</summary>
    public Counter<long> OrderPlacedTotal { get; }

    // ── Plugins de Catálogo ──────────────────────────────────────────────────
    /// <summary>Buscas por plugin (tag: plugin)</summary>
    public Counter<long> PluginSearchTotal { get; }

    /// <summary>Erros por plugin (tag: plugin)</summary>
    public Counter<long> PluginErrorTotal { get; }

    /// <summary>Fallbacks ativados (plugin retornou 0 itens ou lançou) (tag: plugin)</summary>
    public Counter<long> PluginFallbackTotal { get; }

    /// <summary>Latência por plugin em ms (tag: plugin)</summary>
    public Histogram<double> PluginDurationMs { get; }

    // ── Cache Redis ──────────────────────────────────────────────────────────
    /// <summary>Cache hits (tag: cache_key_prefix)</summary>
    public Counter<long> CacheHitTotal { get; }

    /// <summary>Cache misses (tag: cache_key_prefix)</summary>
    public Counter<long> CacheMissTotal { get; }

    // ── LLM / Ollama ─────────────────────────────────────────────────────────
    /// <summary>Intenções detectadas pelo router (tag: intent)</summary>
    public Counter<long> IntentDetectedTotal { get; }

    /// <summary>Chamadas ao Ollama para análise K6 (tag: model)</summary>
    public Counter<long> OllamaRequestTotal { get; }

    /// <summary>Erros nas chamadas ao Ollama (tag: model)</summary>
    public Counter<long> OllamaErrorTotal { get; }

    /// <summary>Latência das chamadas ao Ollama em ms (tag: model)</summary>
    public Histogram<double> OllamaDurationMs { get; }

    public UcpMetrics()
    {
        _meter = new Meter(MeterName, "1.0");

        // Funil UCP
        SearchTotal        = _meter.CreateCounter<long>("ucp.search.total",        "requests", "Total de buscas iniciadas");
        SearchResultsCount = _meter.CreateHistogram<double>("ucp.search.results",  "items",    "Itens retornados por busca");
        SearchDurationMs   = _meter.CreateHistogram<double>("ucp.search.duration", "ms",       "Latência da busca agregada");
        CartAddTotal       = _meter.CreateCounter<long>("ucp.cart.add.total",       "items",    "Produtos adicionados ao carrinho");
        CheckoutTotal      = _meter.CreateCounter<long>("ucp.checkout.total",       "requests", "Checkouts iniciados");
        CheckoutSuccessTotal = _meter.CreateCounter<long>("ucp.checkout.success",   "requests", "Checkouts concluídos");
        CheckoutFailureTotal = _meter.CreateCounter<long>("ucp.checkout.failure",   "requests", "Checkouts com falha");
        OrderPlacedTotal   = _meter.CreateCounter<long>("ucp.order.placed.total",   "orders",   "Pedidos criados");

        // Plugins
        PluginSearchTotal   = _meter.CreateCounter<long>("ucp.plugin.search.total",    "requests", "Buscas por plugin");
        PluginErrorTotal    = _meter.CreateCounter<long>("ucp.plugin.error.total",     "errors",   "Erros por plugin");
        PluginFallbackTotal = _meter.CreateCounter<long>("ucp.plugin.fallback.total",  "events",   "Fallbacks ativados por plugin");
        PluginDurationMs    = _meter.CreateHistogram<double>("ucp.plugin.duration",    "ms",       "Latência por plugin");

        // Cache
        CacheHitTotal  = _meter.CreateCounter<long>("ucp.cache.hit.total",  "hits",   "Cache hits");
        CacheMissTotal = _meter.CreateCounter<long>("ucp.cache.miss.total", "misses", "Cache misses");

        // LLM / Intent
        IntentDetectedTotal = _meter.CreateCounter<long>("ucp.intent.detected.total", "requests", "Intenções detectadas");
        OllamaRequestTotal  = _meter.CreateCounter<long>("ucp.ollama.request.total",  "requests", "Chamadas ao Ollama");
        OllamaErrorTotal    = _meter.CreateCounter<long>("ucp.ollama.error.total",    "errors",   "Erros nas chamadas ao Ollama");
        OllamaDurationMs    = _meter.CreateHistogram<double>("ucp.ollama.duration",   "ms",       "Latência Ollama");
    }

    public void Dispose() => _meter.Dispose();
}
