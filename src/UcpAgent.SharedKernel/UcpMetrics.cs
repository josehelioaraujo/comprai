using System.Diagnostics.Metrics;
using System.Diagnostics.CodeAnalysis;

namespace UcpAgent.SharedKernel;

/// <summary>
/// Instrumentos OTel centralizados para o Funil UCP, Plugins, Cache Redis e LLM/Ollama.
/// Nomes com underscore para compatibilidade com PromQL (Prometheus 3.x não aceita ponto em seletores).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UcpMetrics : IDisposable
{
    public const string MeterName = "comprai.ucp";

    private readonly Meter _meter;

    // ── Funil UCP ────────────────────────────────────────────────────────────
    public Counter<long>    SearchTotal        { get; }
    public Histogram<double> SearchResultsCount { get; }
    public Histogram<double> SearchDurationMs   { get; }
    public Counter<long>    CartAddTotal        { get; }
    public Counter<long>    CheckoutTotal       { get; }
    public Counter<long>    CheckoutSuccessTotal { get; }
    public Counter<long>    CheckoutFailureTotal { get; }
    public Counter<long>    OrderPlacedTotal    { get; }

    // ── Plugins ───────────────────────────────────────────────────────────────
    public Counter<long>    PluginSearchTotal   { get; }
    public Counter<long>    PluginErrorTotal    { get; }
    public Counter<long>    PluginFallbackTotal { get; }
    public Histogram<double> PluginDurationMs   { get; }

    // ── Cache Redis ───────────────────────────────────────────────────────────
    public Counter<long> CacheHitTotal  { get; }
    public Counter<long> CacheMissTotal { get; }

    // ── LLM / Ollama ──────────────────────────────────────────────────────────
    public Counter<long>    IntentDetectedTotal { get; }
    public Counter<long>    OllamaRequestTotal  { get; }
    public Counter<long>    OllamaErrorTotal    { get; }
    public Histogram<double> OllamaDurationMs   { get; }

    public UcpMetrics()
    {
        _meter = new Meter(MeterName, "1.0");

        // Funil UCP — underscore para PromQL funcionar no Prometheus 3.x
        SearchTotal          = _meter.CreateCounter<long>("ucp_search",            "requests", "Total de buscas iniciadas");
        SearchResultsCount   = _meter.CreateHistogram<double>("ucp_search_results",    "items",    "Itens retornados por busca");
        SearchDurationMs     = _meter.CreateHistogram<double>("ucp_search_duration",   "ms",       "Latência da busca agregada");
        CartAddTotal         = _meter.CreateCounter<long>("ucp_cart_add",          "items",    "Produtos adicionados ao carrinho");
        CheckoutTotal        = _meter.CreateCounter<long>("ucp_checkout",          "requests", "Checkouts iniciados");
        CheckoutSuccessTotal = _meter.CreateCounter<long>("ucp_checkout_success",  "requests", "Checkouts concluídos");
        CheckoutFailureTotal = _meter.CreateCounter<long>("ucp_checkout_failure",  "requests", "Checkouts com falha");
        OrderPlacedTotal     = _meter.CreateCounter<long>("ucp_order_placed",      "orders",   "Pedidos criados");

        // Plugins
        PluginSearchTotal   = _meter.CreateCounter<long>("ucp_plugin_search",     "requests", "Buscas por plugin");
        PluginErrorTotal    = _meter.CreateCounter<long>("ucp_plugin_error",      "errors",   "Erros por plugin");
        PluginFallbackTotal = _meter.CreateCounter<long>("ucp_plugin_fallback",   "events",   "Fallbacks por plugin");
        PluginDurationMs    = _meter.CreateHistogram<double>("ucp_plugin_duration",   "ms",       "Latência por plugin");

        // Cache
        CacheHitTotal  = _meter.CreateCounter<long>("ucp_cache_hit",         "hits",   "Cache hits");
        CacheMissTotal = _meter.CreateCounter<long>("ucp_cache_miss",        "misses", "Cache misses");

        // LLM / Intent
        IntentDetectedTotal = _meter.CreateCounter<long>("ucp_intent_detected",   "requests", "Intenções detectadas");
        OllamaRequestTotal  = _meter.CreateCounter<long>("ucp_ollama_request",    "requests", "Chamadas ao Ollama");
        OllamaErrorTotal    = _meter.CreateCounter<long>("ucp_ollama_error",      "errors",   "Erros Ollama");
        OllamaDurationMs    = _meter.CreateHistogram<double>("ucp_ollama_duration",   "ms",       "Latência Ollama");
    }

    public void Dispose() => _meter.Dispose();
}

