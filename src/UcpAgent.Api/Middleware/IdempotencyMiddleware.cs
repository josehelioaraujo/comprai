using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UcpAgent.Infrastructure.Persistence;

namespace UcpAgent.Api.Middleware;

/// <summary>
/// Middleware de idempotência via PostgreSQL (F7).
/// Lê/grava idempotency_key no BD com TTL 24h.
/// Fallback transparente se BD indisponível.
/// </summary>
public sealed class IdempotencyMiddleware(
    RequestDelegate next,
    IDbConnectionFactory? factory,
    ILogger<IdempotencyMiddleware> logger)
{
    private const string Header = "X-Idempotency-Key";

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (factory is null ||
            !ctx.Request.Headers.TryGetValue(Header, out var keyValue) ||
            string.IsNullOrWhiteSpace(keyValue))
        {
            await next(ctx);
            return;
        }

        var key    = keyValue.ToString().Trim();
        var path   = ctx.Request.Path.Value ?? "";
        var method = ctx.Request.Method;

        if (method is "GET" or "HEAD" or "OPTIONS")
        {
            await next(ctx);
            return;
        }

        // Rastreia se o endpoint já foi executado para evitar double-execution no catch
        var endpointExecuted = false;

        try
        {
            await using var conn = await factory.CreateAsync(ctx.RequestAborted);

            var existing = await conn.QuerySingleOrDefaultAsync<IdempotencyRecord>("""
                SELECT status_code AS StatusCode, response_body AS ResponseBody
                  FROM idempotency_key
                 WHERE key = @key AND path = @path AND expires_at > NOW()
                """, new { key, path });

            if (existing is not null)
            {
                logger.LogInformation("[Idempotency] Cache hit: {Key}", key);
                ctx.Response.StatusCode  = existing.StatusCode;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(existing.ResponseBody, ctx.RequestAborted);
                return;
            }

            var originalBody = ctx.Response.Body;
            using var buffer = new MemoryStream();
            ctx.Response.Body = buffer;

            endpointExecuted = true;
            await next(ctx);

            // Restaura body ANTES de qualquer operação de BD que possa falhar
            buffer.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(buffer).ReadToEndAsync();
            buffer.Seek(0, SeekOrigin.Begin);
            await buffer.CopyToAsync(originalBody, ctx.RequestAborted);
            ctx.Response.Body = originalBody;

            if (ctx.Response.StatusCode is >= 200 and < 300)
            {
                // INSERT em try separado: falha aqui não deve re-executar o endpoint
                try
                {
                    await conn.ExecuteAsync("""
                        INSERT INTO idempotency_key (key, path, method, status_code, response_body, expires_at)
                        VALUES (@key, @path, @method, @statusCode, @responseBody, NOW() + INTERVAL '24 hours')
                        ON CONFLICT (key, path) DO NOTHING
                        """,
                        new { key, path, method, statusCode = ctx.Response.StatusCode, responseBody });
                }
                catch (Exception insertEx)
                {
                    // Resposta já enviada ao cliente — apenas loga, não re-executa
                    logger.LogWarning(insertEx, "[Idempotency] Falha ao gravar cache — key {Key} já respondida ao cliente", key);
                }
            }
        }
        catch (Exception ex) when (!endpointExecuted)
        {
            // Falha antes do endpoint executar (SELECT, CreateAsync) — fallback seguro
            logger.LogWarning(ex, "[Idempotency] Falha no BD antes do endpoint — passando sem idempotência");
            await next(ctx);
        }
        catch (Exception ex)
        {
            // Falha durante/após next(ctx) — resposta pode estar em estado indeterminado
            // Não re-executa o endpoint para evitar double-execution
            logger.LogWarning(ex, "[Idempotency] Falha após execução do endpoint — {Key}", key);
        }
    }

    private sealed record IdempotencyRecord(int StatusCode, string ResponseBody);
}