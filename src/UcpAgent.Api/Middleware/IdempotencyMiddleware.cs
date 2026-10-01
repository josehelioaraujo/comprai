using System.Collections.Concurrent;
using StackExchange.Redis;

namespace UcpAgent.Api.Middleware;

/// <summary>
/// Garante idempotência nos endpoints de escrita via header X-Idempotency-Key.
///
/// Funciona SEMPRE — Redis é upgrade de durabilidade, não requisito:
///   - Com Redis:  cache distribuído, TTL 24h, sobrevive restart da aplicação
///   - Sem Redis:  cache em memória, TTL 30min, escopo do processo
///
/// A mesma X-Idempotency-Key NUNCA processa duas vezes, independente de
/// infraestrutura — consistência de pedido garantida em qualquer ambiente.
///
/// Endpoints cobertos: POST /api/cart/*, /api/checkout/*, /api/payment/*
/// </summary>
public sealed class IdempotencyMiddleware(
    RequestDelegate next,
    IServiceProvider sp,
    ILogger<IdempotencyMiddleware> logger)
{
    private static readonly string[] IdempotentPaths =
        ["/api/cart", "/api/checkout", "/api/payment"];

    private static readonly TimeSpan RedisTtl  = TimeSpan.FromHours(24);
    private static readonly TimeSpan MemoryTtl = TimeSpan.FromMinutes(30);

    // Cache em memória — singleton, vive com o processo
    private static readonly ConcurrentDictionary<string, (string Body, DateTimeOffset Expires)>
        _memCache = new();
    private static DateTimeOffset _lastCleanup = DateTimeOffset.UtcNow;

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (ctx.Request.Method != HttpMethods.Post
            || !IdempotentPaths.Any(p => ctx.Request.Path.StartsWithSegments(p)))
        {
            await next(ctx);
            return;
        }

        var key = ctx.Request.Headers["X-Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(key))
        {
            await next(ctx);
            return;
        }

        var redis = sp.GetService<IConnectionMultiplexer>();

        if (redis is not null)
            await HandleWithRedisAsync(ctx, redis, key);
        else
            await HandleWithMemoryAsync(ctx, key);
    }

    // ── Redis ─────────────────────────────────────────────────────────────────

    private async Task HandleWithRedisAsync(
        HttpContext ctx, IConnectionMultiplexer redis, string key)
    {
        var redisKey = $"idempotency:{key}";
        var db       = redis.GetDatabase();

        var cached = await db.StringGetAsync(redisKey);
        if (cached.HasValue)
        {
            logger.LogInformation("[Idempotency/Redis] Hit key={Key}", key);
            await ReturnCachedAsync(ctx, cached.ToString());
            return;
        }

        var body = await CaptureAsync(ctx);
        if (body is not null)
        {
            await db.StringSetAsync(redisKey, body, RedisTtl);
            logger.LogInformation("[Idempotency/Redis] Stored key={Key} TTL=24h", key);
        }
    }

    // ── Memória (fallback) ────────────────────────────────────────────────────

    private async Task HandleWithMemoryAsync(HttpContext ctx, string key)
    {
        PurgeExpired();

        if (_memCache.TryGetValue(key, out var hit) && hit.Expires > DateTimeOffset.UtcNow)
        {
            logger.LogInformation("[Idempotency/Memory] Hit key={Key}", key);
            await ReturnCachedAsync(ctx, hit.Body);
            return;
        }

        var body = await CaptureAsync(ctx);
        if (body is not null)
        {
            _memCache[key] = (body, DateTimeOffset.UtcNow.Add(MemoryTtl));
            logger.LogInformation("[Idempotency/Memory] Stored key={Key} TTL=30min", key);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task ReturnCachedAsync(HttpContext ctx, string body)
    {
        ctx.Response.StatusCode  = 200;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync(body);
    }

    /// <summary>
    /// Executa o pipeline e captura o body da resposta.
    /// Retorna null se a resposta não foi 2xx.
    /// </summary>
    private async Task<string?> CaptureAsync(HttpContext ctx)
    {
        var originalBody = ctx.Response.Body;
        using var buffer = new MemoryStream();
        ctx.Response.Body = buffer;

        await next(ctx);   // executa o restante do pipeline

        buffer.Position  = 0;
        var responseBody = await new StreamReader(buffer).ReadToEndAsync();

        // Devolve o body ao stream original
        buffer.Position = 0;
        await buffer.CopyToAsync(originalBody);
        ctx.Response.Body = originalBody;

        return ctx.Response.StatusCode is >= 200 and < 300 ? responseBody : null;
    }

    /// <summary>Remove entradas expiradas do cache em memória (a cada 5 min).</summary>
    private static void PurgeExpired()
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastCleanup < TimeSpan.FromMinutes(5)) return;
        _lastCleanup = now;

        foreach (var k in _memCache.Keys)
            if (_memCache.TryGetValue(k, out var e) && e.Expires <= now)
                _memCache.TryRemove(k, out _);
    }
}
