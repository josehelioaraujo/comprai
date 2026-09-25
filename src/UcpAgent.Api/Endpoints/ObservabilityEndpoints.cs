using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using UcpAgent.SharedKernel;

namespace UcpAgent.Api.Endpoints;

[ExcludeFromCodeCoverage]
public static class ObservabilityEndpoints
{
    public static void MapObservabilityEndpoints(this WebApplication app)
    {
        // ── Métricas — Visão Geral (Prometheus) ───────────────────────────────
        app.MapGet("/api/observability/metrics", async (
            string? range,
            IConfiguration config,
            IHttpClientFactory factory,
            CancellationToken ct) =>
        {
            var baseUrl = config["Observability:PrometheusUrl"] ?? "http://comprai-prometheus:9090";
            var step    = RangeToStep(range ?? "15m");
            var end     = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var start   = end - RangeToSeconds(range ?? "15m");
            var client  = factory.CreateClient("observability");

            var qRps   = "rate({__name__=\"http.server.request.duration_seconds_count\"}[2m])";
            var qP99   = "histogram_quantile(0.99, rate({__name__=\"http.server.request.duration_seconds_bucket\"}[5m])) * 1000";
            var qErr   = "rate({__name__=\"http.server.request.duration_seconds_count\",http_response_status_code=~\"5..\"}[2m]) / rate({__name__=\"http.server.request.duration_seconds_count\"}[2m]) * 100";

            var rps     = await QueryInstant(client, baseUrl, qRps, ct);
            var p99Raw  = await QueryInstant(client, baseUrl, qP99, ct);
            var errRate = await QueryInstant(client, baseUrl, qErr, ct);
            var series  = await QueryRange(client, baseUrl, qRps, start, end, step, ct);
            var seriesP99 = await QueryRange(client, baseUrl, qP99, start, end, step, ct);

            return Results.Ok(new
            {
                rps, p99 = p99Raw, errorRate = errRate, uptime = 99.9,
                series = new[] { new { timestamps = series.labels, rps = series.values, p99 = seriesP99.values } }
            });
        })
        .WithTags("Observability").WithName("ObsMetrics").AllowAnonymous();

        // ── Métricas — Funil UCP ──────────────────────────────────────────────
        app.MapGet("/api/observability/metrics/funil", async (
            IConfiguration config,
            IHttpClientFactory factory,
            UcpMetrics metrics,
            CancellationToken ct) =>
        {
            var baseUrl = config["Observability:PrometheusUrl"] ?? "http://comprai-prometheus:9090";
            var client  = factory.CreateClient("observability");

            var search   = await QueryInstant(client, baseUrl, "sum(ucp_search_total_total)",    ct);
            var cart     = await QueryInstant(client, baseUrl, "sum(ucp_cart_add_total_items_total)",   ct);
            var checkout = await QueryInstant(client, baseUrl, "sum(ucp_checkout_success_total)", ct);
            var orders   = await QueryInstant(client, baseUrl, "sum(ucp_order_placed_total_orders_total)", ct);
            var chkFail  = await QueryInstant(client, baseUrl, "sum(ucp_checkout_total_total)", ct);

            return Results.Ok(new
            {
                search   = search   ?? 0,
                cart     = cart     ?? 0,
                checkout = checkout ?? 0,
                orders   = orders   ?? 0,
                checkoutFailure = chkFail ?? 0
            });
        })
        .WithTags("Observability").WithName("ObsFunil").AllowAnonymous();

        // ── Métricas — Plugins ────────────────────────────────────────────────
        app.MapGet("/api/observability/metrics/plugins", async (
            IConfiguration config,
            IHttpClientFactory factory,
            CancellationToken ct) =>
        {
            var baseUrl = config["Observability:PrometheusUrl"] ?? "http://comprai-prometheus:9090";
            var client  = factory.CreateClient("observability");

            // Buscar totais por plugin (label: plugin)
            var totalByPlugin   = await QueryLabeled(client, baseUrl, "sum by (plugin)(ucp_plugin_search_total_total)",   "plugin", ct);
            var errorsByPlugin  = await QueryLabeled(client, baseUrl, "sum by (plugin)(ucp_plugin_fallback_total_total)",    "plugin", ct);
            var fallbackByPlugin= await QueryLabeled(client, baseUrl, "sum by (plugin)(ucp_plugin_fallback_total_total)", "plugin", ct);
            var p95ByPlugin     = await QueryLabeled(client, baseUrl, "histogram_quantile(0.95, sum by (plugin, le)(rate(ucp_plugin_duration_milliseconds_bucket[5m])))", "plugin", ct);

            var plugins = totalByPlugin.Keys.Union(errorsByPlugin.Keys).Distinct().Select(p => new
            {
                plugin    = p,
                total     = (int)(totalByPlugin.GetValueOrDefault(p, 0)),
                errors    = (int)(errorsByPlugin.GetValueOrDefault(p, 0)),
                fallbacks = (int)(fallbackByPlugin.GetValueOrDefault(p, 0)),
                p95       = p95ByPlugin.ContainsKey(p) ? (double?)p95ByPlugin[p] : null
            }).OrderByDescending(x => x.total).ToList();

            return Results.Ok(new { plugins });
        })
        .WithTags("Observability").WithName("ObsPlugins").AllowAnonymous();

        // ── Métricas — Cache Redis ────────────────────────────────────────────
        app.MapGet("/api/observability/metrics/cache", async (
            IConfiguration config,
            IHttpClientFactory factory,
            CancellationToken ct) =>
        {
            var baseUrl = config["Observability:PrometheusUrl"] ?? "http://comprai-prometheus:9090";
            var client  = factory.CreateClient("observability");

            var hits   = await QueryInstant(client, baseUrl, "sum(ucp_cache_hit_total_hits_total)",  ct);
            var misses = await QueryInstant(client, baseUrl, "sum(ucp_cache_miss_total_misses_total)", ct);
            var total  = (hits ?? 0) + (misses ?? 0);
            double? hitRate = total > 0 ? ((hits ?? 0) / total * 100) : null;

            return Results.Ok(new { hits, misses, hitRate });
        })
        .WithTags("Observability").WithName("ObsCache").AllowAnonymous();

        // ── Métricas — LLM / Ollama ───────────────────────────────────────────
        app.MapGet("/api/observability/metrics/llm", async (
            IConfiguration config,
            IHttpClientFactory factory,
            CancellationToken ct) =>
        {
            var baseUrl = config["Observability:PrometheusUrl"] ?? "http://comprai-prometheus:9090";
            var client  = factory.CreateClient("observability");

            var totalIntents  = await QueryInstant(client, baseUrl, "sum(ucp_intent_detected_total_total)", ct);
            var ollamaCalls   = await QueryInstant(client, baseUrl, "sum(ucp_ollama_request_total_total)",  ct);
            var ollamaErrors  = await QueryInstant(client, baseUrl, "sum(ucp_ollama_error_total_errors_total)",    ct);
            var avgLatencyMs  = await QueryInstant(client, baseUrl,
                "sum(rate(ucp_ollama_duration_milliseconds_sum[5m])) / sum(rate(ucp_ollama_duration_milliseconds_count[5m]))", ct);

            // Distribuição de intenções por label
            var intentDist = await QueryLabeled(client, baseUrl, "sum by (intent)(ucp_intent_detected_total_total)", "intent", ct);

            return Results.Ok(new { totalIntents, ollamaCalls, ollamaErrors, avgLatencyMs, intentDistribution = intentDist });
        })
        .WithTags("Observability").WithName("ObsLlm").AllowAnonymous();

        // ── Logs — Loki ───────────────────────────────────────────────────────
        app.MapGet("/api/observability/logs", async (
            string? level, string? q, int? limit,
            IConfiguration config, IHttpClientFactory factory, CancellationToken ct) =>
        {
            var baseUrl = config["Observability:LokiUrl"] ?? "http://comprai-loki:3100";
            var n       = limit ?? 100;
            var client  = factory.CreateClient("observability");
            var levelFilter = string.IsNullOrWhiteSpace(level) || level == "all" ? "" : $", level=\"{level}\"";
            var logQuery = $"{{job=\"comprai-api\"{levelFilter}}}";
            if (!string.IsNullOrWhiteSpace(q)) logQuery += $" |= `{q}`";

            var url = $"{baseUrl}/loki/api/v1/query_range" +
                      $"?query={Uri.EscapeDataString(logQuery)}&limit={n}" +
                      $"&start={DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeMilliseconds()}000000" +
                      $"&end={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}000000&direction=backward";
            try
            {
                var resp = await client.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) return Results.Ok(new { entries = Array.Empty<object>() });
                var json = await resp.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                var entries = new List<object>();
                if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("result", out var result))
                {
                    foreach (var stream in result.EnumerateArray())
                    {
                        var labels = stream.TryGetProperty("stream", out var s) ? s : default;
                        var lvl = labels.ValueKind != JsonValueKind.Undefined && labels.TryGetProperty("level", out var lv) ? lv.GetString() : "info";
                        if (!stream.TryGetProperty("values", out var values)) continue;
                        foreach (var entry in values.EnumerateArray())
                        {
                            var arr = entry.EnumerateArray().ToList();
                            if (arr.Count < 2) continue;
                            var tsNs = long.TryParse(arr[0].GetString(), out var ns) ? ns : 0;
                            var ts = DateTimeOffset.FromUnixTimeMilliseconds(tsNs / 1_000_000).UtcDateTime;
                            var msg = arr[1].GetString() ?? "";
                            var parsedLevel = lvl;
                            try { using var ld = JsonDocument.Parse(msg); if (ld.RootElement.TryGetProperty("level", out var ll)) parsedLevel = ll.GetString(); if (ld.RootElement.TryGetProperty("message", out var lm)) msg = lm.GetString() ?? msg; } catch { }
                            entries.Add(new { timestamp = ts, level = parsedLevel, message = msg });
                        }
                    }
                }
                return Results.Ok(new { entries });
            }
            catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 503); }
        })
        .WithTags("Observability").WithName("ObsLogs").AllowAnonymous();

        // ── Traces — Jaeger ───────────────────────────────────────────────────
        app.MapGet("/api/observability/traces", async (
            string? service, int? limit,
            IConfiguration config, IHttpClientFactory factory, CancellationToken ct) =>
        {
            var baseUrl = config["Observability:JaegerUrl"] ?? "http://comprai-jaeger:16686";
            var svc = service ?? "comprai-api";
            var n = limit ?? 20;
            var client = factory.CreateClient("observability");
            var url = $"{baseUrl}/api/traces?service={Uri.EscapeDataString(svc)}&limit={n}";
            try
            {
                var resp = await client.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) return Results.Ok(new { traces = Array.Empty<object>() });
                var json = await resp.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                var traces = new List<object>();
                if (doc.RootElement.TryGetProperty("data", out var data))
                {
                    foreach (var trace in data.EnumerateArray().Take(n))
                    {
                        if (!trace.TryGetProperty("spans", out var spansEl)) continue;
                        var spans = spansEl.EnumerateArray().ToList();
                        if (!spans.Any()) continue;
                        var root = spans.FirstOrDefault(s => !s.TryGetProperty("references", out var refs) || !refs.EnumerateArray().Any(r => r.TryGetProperty("refType", out var rt) && rt.GetString() == "CHILD_OF"));
                        var opName = root.TryGetProperty("operationName", out var op) ? op.GetString() : "unknown";
                        var dur = root.TryGetProperty("duration", out var d) ? d.GetInt64() / 1000 : 0;
                        var hasErr = spans.Any(s => s.TryGetProperty("tags", out var tags) && tags.EnumerateArray().Any(t => t.TryGetProperty("key", out var k) && k.GetString() == "error" && t.TryGetProperty("value", out var v) && v.GetRawText() == "true"));
                        traces.Add(new { operation = opName, service = svc, duration = dur, error = hasErr, spans = spans.Select(s => new { operation = s.TryGetProperty("operationName", out var sop) ? sop.GetString() : "", duration = s.TryGetProperty("duration", out var sd) ? sd.GetInt64() / 1000 : 0 }).ToList() });
                    }
                }
                return Results.Ok(new { traces });
            }
            catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 503); }
        })
        .WithTags("Observability").WithName("ObsTraces").AllowAnonymous();

        // ── Erros — Loki ─────────────────────────────────────────────────────
        app.MapGet("/api/observability/errors", async (
            int? limit,
            IConfiguration config, IHttpClientFactory factory, CancellationToken ct) =>
        {
            var baseUrl = config["Observability:LokiUrl"] ?? "http://comprai-loki:3100";
            var n = limit ?? 20;
            var client = factory.CreateClient("observability");
            var logQuery = "{job=\"comprai-api\"} |= `` | json | level=~\"error|fatal|Error|Fatal\"";
            var url = $"{baseUrl}/loki/api/v1/query_range?query={Uri.EscapeDataString(logQuery)}&limit=200" +
                      $"&start={DateTimeOffset.UtcNow.AddHours(-6).ToUnixTimeMilliseconds()}000000" +
                      $"&end={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}000000&direction=backward";
            try
            {
                var resp = await client.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) return Results.Ok(new { errors = Array.Empty<object>() });
                var json = await resp.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                var grouped = new Dictionary<string, (int count, DateTime lastSeen, string stacktrace)>();
                if (doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("result", out var result))
                {
                    foreach (var stream in result.EnumerateArray())
                    {
                        if (!stream.TryGetProperty("values", out var values)) continue;
                        foreach (var entry in values.EnumerateArray())
                        {
                            var arr = entry.EnumerateArray().ToList();
                            if (arr.Count < 2) continue;
                            var tsNs = long.TryParse(arr[0].GetString(), out var ns) ? ns : 0;
                            var ts = DateTimeOffset.FromUnixTimeMilliseconds(tsNs / 1_000_000).UtcDateTime;
                            var raw = arr[1].GetString() ?? "";
                            var msg = raw; var stack = "";
                            try { using var ld = JsonDocument.Parse(raw); if (ld.RootElement.TryGetProperty("message", out var lm)) msg = lm.GetString() ?? raw; if (ld.RootElement.TryGetProperty("exception", out var le)) stack = le.GetString() ?? ""; } catch { }
                            var key = msg.Length > 120 ? msg[..120] : msg;
                            if (grouped.TryGetValue(key, out var existing)) grouped[key] = (existing.count + 1, ts > existing.lastSeen ? ts : existing.lastSeen, existing.stacktrace);
                            else grouped[key] = (1, ts, stack);
                        }
                    }
                }
                var errors = grouped.OrderByDescending(g => g.Value.lastSeen).Take(n).Select(g => new { message = g.Key, count = g.Value.count, lastSeen = g.Value.lastSeen, stacktrace = g.Value.stacktrace }).ToList();
                return Results.Ok(new { errors });
            }
            catch (Exception ex) { return Results.Problem(ex.Message, statusCode: 503); }
        })
        .WithTags("Observability").WithName("ObsErrors").AllowAnonymous();
    }

    // ── Helpers Prometheus ────────────────────────────────────────────────────
    private static async Task<double?> QueryInstant(HttpClient client, string baseUrl, string query, CancellationToken ct)
    {
        try
        {
            var resp = await client.GetAsync($"{baseUrl}/api/v1/query?query={Uri.EscapeDataString(query)}", ct);
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var result = doc.RootElement.GetProperty("data").GetProperty("result").EnumerateArray().FirstOrDefault();
            if (result.ValueKind == JsonValueKind.Undefined) return null;
            var val = result.GetProperty("value").EnumerateArray().Skip(1).FirstOrDefault();
            if (!double.TryParse(val.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d)) return null;
            return double.IsFinite(d) ? d : null;
        }
        catch { return null; }
    }

    // Retorna dict<labelValue, metricValue> para queries com groupby
    private static async Task<Dictionary<string, double>> QueryLabeled(HttpClient client, string baseUrl, string query, string labelName, CancellationToken ct)
    {
        var result = new Dictionary<string, double>();
        try
        {
            var resp = await client.GetAsync($"{baseUrl}/api/v1/query?query={Uri.EscapeDataString(query)}", ct);
            if (!resp.IsSuccessStatusCode) return result;
            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            foreach (var item in doc.RootElement.GetProperty("data").GetProperty("result").EnumerateArray())
            {
                if (!item.TryGetProperty("metric", out var metric)) continue;
                if (!metric.TryGetProperty(labelName, out var lv)) continue;
                var key = lv.GetString() ?? "";
                var val = item.GetProperty("value").EnumerateArray().Skip(1).FirstOrDefault();
                if (double.TryParse(val.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d) && double.IsFinite(d))
                    result[key] = d;
            }
        }
        catch { }
        return result;
    }

    private static async Task<(List<string> labels, List<double?> values)> QueryRange(
        HttpClient client, string baseUrl, string query, long start, long end, int step, CancellationToken ct)
    {
        try
        {
            var resp = await client.GetAsync($"{baseUrl}/api/v1/query_range?query={Uri.EscapeDataString(query)}&start={start}&end={end}&step={step}", ct);
            if (!resp.IsSuccessStatusCode) return ([], []);
            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var result = doc.RootElement.GetProperty("data").GetProperty("result").EnumerateArray().FirstOrDefault();
            if (result.ValueKind == JsonValueKind.Undefined) return ([], []);
            var labels = new List<string>(); var values = new List<double?>();
            foreach (var point in result.GetProperty("values").EnumerateArray())
            {
                var pts = point.EnumerateArray().ToList();
                labels.Add(DateTimeOffset.FromUnixTimeSeconds((long)pts[0].GetDouble()).ToLocalTime().ToString("HH:mm"));
                if (double.TryParse(pts[1].GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) && double.IsFinite(v)) values.Add(v);
                else values.Add(null);
            }
            return (labels, values);
        }
        catch { return ([], []); }
    }

    private static int RangeToSeconds(string range) => range switch { "15m" => 900, "1h" => 3600, "6h" => 21600, "24h" => 86400, _ => 900 };
    private static int RangeToStep(string range)    => range switch { "15m" => 30,  "1h" => 60,   "6h" => 300,   "24h" => 900,  _ => 30  };
}

