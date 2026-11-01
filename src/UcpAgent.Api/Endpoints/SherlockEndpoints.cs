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
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Input))
                return Results.BadRequest(new { error = "Input é obrigatório." });

            var skillMd = await GetSkillMdAsync(config, factory, cache, ct);
            var systemPrompt = BuildSystemPrompt(skillMd);
            var userMessage  = BuildUserMessage(req);

            var useGroq = config.GetValue<bool>("Sherlock:UseGroq", true);

            return Results.Stream(
                async stream =>
                {
                    if (useGroq)
                        await StreamGroq(stream, systemPrompt, userMessage, config, factory, ct);
                    else
                        await StreamOllama(stream, systemPrompt, userMessage, config, factory, ct);
                },
                contentType: "text/event-stream",
                enableRangeProcessing: false);
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

    // ── Monta system prompt combinando SKILL.md + contexto Comprai ───────────
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
        - Emita eventos SSE: `data: <chunk>\n\n`
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
        CancellationToken ct)
    {
        var apiKey = config["Sherlock:Groq:ApiKey"] ?? "";
        var model  = config["Sherlock:Groq:Model"]  ?? "llama-3.3-70b-versatile";
        var client = factory.CreateClient("groq");

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
            var err = await resp.Content.ReadAsStringAsync(ct);
            var msg = $"data: {{\"error\":\"Groq retornou {(int)resp.StatusCode}: {err.Replace("\"", "\\\"")}\"}}\n\n";
            await output.WriteAsync(Encoding.UTF8.GetBytes(msg), ct);
            return;
        }

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;
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

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
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
    string? Mode   // "express" para P0/P1
);
