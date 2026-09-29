using StackExchange.Redis;

namespace UcpAgent.Api.Middleware;

/// <summary>
/// Garante idempotência nos endpoints de escrita via header X-Idempotency-Key.
/// Segunda chamada com mesmo key retorna resposta cacheada sem reprocessar.
/// TTL: 24h no Redis.
/// </summary>
public sealed class IdempotencyMiddleware(
    RequestDelegate next,
    IConnectionMultiplexer? redis,
    ILogger<IdempotencyMiddleware> logger)
{
    private static readonly string[] _idempotentPaths = ["/api/cart", "/api/checkout", "/api/payment"];
    private static readonly TimeSpan _ttl = TimeSpan.FromHours(24);

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (ctx.Request.Method != HttpMethods.Post
            || !_idempotentPaths.Any(p => ctx.Request.Path.StartsWithSegments(p))
            || redis is null)
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

        var redisKey = $"idempotency:{key}";
        var db       = redis.GetDatabase();

        // Retorna resposta cacheada se key já foi processada
        var cached = await db.StringGetAsync(redisKey);
        if (cached.HasValue)
        {
            logger.LogInformation("[Idempotency] Key {Key} já processada — cache hit", key);
            ctx.Response.StatusCode  = 200;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(cached.ToString());
            return;
        }

        // Captura a resposta para cachear
        var originalBody = ctx.Response.Body;
        using var buffer = new MemoryStream();
        ctx.Response.Body = buffer;

        await next(ctx);

        buffer.Position = 0;
        var responseBody = await new StreamReader(buffer).ReadToEndAsync();

        // Cacheia só respostas de sucesso (2xx)
        if (ctx.Response.StatusCode >= 200 && ctx.Response.StatusCode < 300)
        {
            await db.StringSetAsync(redisKey, responseBody, _ttl);
            logger.LogInformation("[Idempotency] Key {Key} cacheada (TTL 24h)", key);
        }

        buffer.Position = 0;
        await buffer.CopyToAsync(originalBody);
        ctx.Response.Body = originalBody;
    }
}
