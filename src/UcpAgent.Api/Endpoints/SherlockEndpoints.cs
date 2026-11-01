using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;

namespace UcpAgent.Api.Endpoints;

public static class SherlockEndpoints
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static void MapSherlockEndpoints(this WebApplication app)
    {
        // ── POST /api/sherlock/investigate (SSE streaming) ────────────────────
        app.MapPost("/api/sherlock/investigate", async (
            SherlockRequest req,
            IConfiguration config,
            IHttpClientFactory factory,
            IDistributedCache cache,
            HttpContext http,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Input))
            {
                http.Response.StatusCode = 400;
                await http.Response.WriteAsJsonAsync(new { error = "Input é obrigatório." }, ct);
                return;
            }

            var skillMd = await GetSkillMdAsync(config, factory, cache, ct);
            var temDados = TemDadosConcretos(req.Input ?? "");
            var systemPrompt = temDados
                ? BuildSystemPrompt(skillMd)
                : BuildProativeSystemPrompt();
            var userMessage  = BuildUserMessage(req);

            var useGroq    = config.GetValue<bool>("Sherlock:UseGroq", true);
            var modelOverride = !string.IsNullOrWhiteSpace(req.Model) ? req.Model : null;

            http.Response.ContentType  = "text/event-stream";
            http.Response.Headers.CacheControl = "no-cache";

            if (useGroq)
                await StreamGroq(http.Response.Body, systemPrompt, userMessage, config, factory, ct, modelOverride);
            else
                await StreamOllama(http.Response.Body, systemPrompt, userMessage, config, factory, ct);
        })
        .WithTags("Sherlock")
        .WithName("SherlockInvestigate")
        .WithSummary("Investiga causa raiz via Sherlock Skill (SSE streaming)");

        // ── GET /api/sherlock/skill — retorna o SKILL.md atual ───────────────
        app.MapGet("/api/sherlock/skill", async (
            IConfiguration config,
            IHttpClientFactory factory,
            IDistributedCache cache,
            CancellationToken ct) =>
        {
            var md = await GetSkillMdAsync(config, factory, cache, ct);
            return Results.Text(md, "text/markdown");
        })
        .WithTags("Sherlock")
        .WithName("SherlockSkillMd");
    }

    // ── Fetch SKILL.md com cache Redis 1h ────────────────────────────────────
    private static async Task<string> GetSkillMdAsync(
        IConfiguration config,
        IHttpClientFactory factory,
        IDistributedCache cache,
        CancellationToken ct)
    {
        const string cacheKey = "sherlock:skill-md";

        var cached = await cache.GetStringAsync(cacheKey, ct);
        if (cached is not null) return cached;

        var skillUrl = config["Sherlock:SkillUrl"]
            ?? "https://raw.githubusercontent.com/josehelioaraujo/sherlock-skill/main/skill/SKILL.md";

        var client  = factory.CreateClient("sherlock-skill");
        var skillMd = await client.GetStringAsync(skillUrl, ct);

        await cache.SetStringAsync(cacheKey, skillMd,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            }, ct);

        return skillMd;
    }

    // ── Detecta se o input contém dados observáveis concretos ────────────────
    private static bool TemDadosConcretos(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        // Formato estruturado do OpsWatch (Health Map, K6, erros, SINTOMA)
        if (input.Contains("=== HEALTH MAP") || input.Contains("=== K6") ||
            input.Contains("=== ERROS") || input.Contains("=== SINTOMA") ||
            input.Contains("SINTOMA:") || input.Contains("latência="))
            return true;
        // Dados brutos de observabilidade
        return input.Contains("Exception") || input.Contains("ERROR") ||
               input.Contains("stack") || input.Contains("trace") ||
               input.Contains("SELECT") || input.Contains("timeout") ||
               input.Length > 300;
    }

    // ── Monta system prompt — modo investigação (com dados) ──────────────────
    private static string BuildSystemPrompt(string skillMd)
    {
        return $"""
        {skillMd}

        ---
        ## Contexto do ambiente
        - Stack: .NET 10 / C#, Azure AKS, PostgreSQL + Dapper, Redis, Kafka, RabbitMQ
        - Observabilidade: Jaeger (tracing), Grafana + Prometheus (métricas), Loki (logs)
        - Repositório: github.com/josehelioaraujo/comprai
        - Responda sempre em português BR.
        - Use Markdown com headers ## para cada fase.
        """;
    }

    // ── System prompt alternativo — modo proativo (sem dados) ────────────────
    private static string BuildProativeSystemPrompt()
    {
        return """
        Você é o Sherlock, especialista em SRE e diagnóstico de sistemas distribuídos.

        ## Contexto do sistema Comprai
        - Stack: .NET 10 / C#, Azure AKS, PostgreSQL + Dapper, Redis, Kafka, RabbitMQ
        - Observabilidade: Jaeger (tracing), Grafana + Prometheus (métricas), Loki (logs)
        - Repositório: github.com/josehelioaraujo/comprai

        ## Sua missão agora
        O operador não forneceu logs, métricas ou erros concretos.
        Você deve fazer uma **análise proativa de saúde** com base no conhecimento do ambiente.

        ## Formato obrigatório da resposta

        ### 1. Estado atual estimado por serviço
        Para cada serviço (.NET API, Redis, PostgreSQL, Kafka, RabbitMQ, AKS/pods):
        - Principais hipóteses de risco (2-3 por serviço)
        - Confidence score em % para cada hipótese
        - Comando de diagnóstico para confirmar

        ### 2. Mapa de risco
        Tabela markdown: Serviço | Hipótese | Confidence | Comando

        ### 3. Resumo executivo
        Estado geral do sistema (Verde/Amarelo/Vermelho), top 3 riscos, recomendação imediata.

        Responda SEMPRE em português BR com Markdown.
        NÃO peça dados adicionais. Execute a análise diretamente.
        """;
    }

    // ── Monta a mensagem do usuário enriquecida ────────────────────────────────
    private static string BuildUserMessage(SherlockRequest req)
    {
        var sb = new StringBuilder(req.Input);

        if (!string.IsNullOrWhiteSpace(req.Mode) && req.Mode == "express")
            sb.Insert(0, "--express\n\n");

        return sb.ToString();
    }

    // ── Stream via Groq API ────────────────────────────────────────────────────
    private static async Task StreamGroq(
        Stream output,
        string systemPrompt,
        string userMessage,
        IConfiguration config,
        IHttpClientFactory factory,
        CancellationToken ct,
        string? modelOverride = null)
    {
        var apiKey = config["Sherlock:Groq:ApiKey"] ?? "";
        var model  = modelOverride ?? config["Sherlock:Groq:Model"] ?? "openai/gpt-oss-120b";
        var client = factory.CreateClient("groq");

        // diagnóstico: loga primeiros/últimos 4 chars da key (nunca a key completa)
        var keyHint = apiKey.Length >= 8
            ? apiKey[..4] + "…" + apiKey[^4..]
            : apiKey.Length == 0 ? "(vazia)" : "(curta)";
        Console.WriteLine($"[Sherlock] model={model} key={keyHint}");

        var body = JsonSerializer.Serialize(new
        {
            model,
            stream = true,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userMessage  }
            }
        });

        using var req = new HttpRequestMessage(HttpMethod.Post,
            "https://api.groq.com/openai/v1/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var resp = await client.SendAsync(req,
            HttpCompletionOption.ResponseHeadersRead, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var err     = await resp.Content.ReadAsStringAsync(ct);
            var summary = $"Groq HTTP {(int)resp.StatusCode} — {(err.Length > 200 ? err[..200] + "…" : err)}";
            var msg     = JsonSerializer.Serialize(new { error = summary });
            await output.WriteAsync(Encoding.UTF8.GetBytes($"data: {msg}\n\n"), ct);
            return;
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null && !ct.IsCancellationRequested)
        {
            if (!line.StartsWith("data: ")) continue;

            var data = line[6..];
            if (data == "[DONE]")
            {
                await output.WriteAsync("data: [DONE]\n\n"u8.ToArray(), ct);
                break;
            }

            try
            {
                using var doc   = JsonDocument.Parse(data);
                var delta       = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("delta");

                if (delta.TryGetProperty("content", out var content))
                {
                    var chunk = content.GetString() ?? "";
                    var sse   = $"data: {JsonSerializer.Serialize(new { chunk })}\n\n";
                    await output.WriteAsync(Encoding.UTF8.GetBytes(sse), ct);
                    await output.FlushAsync(ct);
                }
            }
            catch { /* chunk malformado — ignora */ }
        }
    }

    // ── Stream via Ollama (fallback local) ────────────────────────────────────
    private static async Task StreamOllama(
        Stream output,
        string systemPrompt,
        string userMessage,
        IConfiguration config,
        IHttpClientFactory factory,
        CancellationToken ct)
    {
        var baseUrl = config["Sherlock:Ollama:BaseUrl"] ?? "http://localhost:11434";
        var model   = config["Sherlock:Ollama:Model"]   ?? "llama3.2:latest";
        var client  = factory.CreateClient("ollama");

        var body = JsonSerializer.Serialize(new
        {
            model,
            stream = true,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userMessage  }
            }
        });

        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"{baseUrl.TrimEnd('/')}/api/chat");
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        using var resp = await client.SendAsync(req,
            HttpCompletionOption.ResponseHeadersRead, ct);

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null && !ct.IsCancellationRequested)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root      = doc.RootElement;

                if (root.TryGetProperty("message", out var msg) &&
                    msg.TryGetProperty("content", out var content))
                {
                    var chunk = content.GetString() ?? "";
                    var sse   = $"data: {JsonSerializer.Serialize(new { chunk })}\n\n";
                    await output.WriteAsync(Encoding.UTF8.GetBytes(sse), ct);
                    await output.FlushAsync(ct);
                }

                if (root.TryGetProperty("done", out var done) && done.GetBoolean())
                {
                    await output.WriteAsync("data: [DONE]\n\n"u8.ToArray(), ct);
                    break;
                }
            }
            catch { /* ignora */ }
        }
    }
}

// ── DTOs ─────────────────────────────────────────────────────────────────────
public record SherlockRequest(
    string Input,
    string? Mode,   // "express" para P0/P1
    string? Model   // override do modelo Groq (ex: "openai/gpt-oss-120b")
);
