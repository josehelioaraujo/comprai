using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Threading.RateLimiting;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace UcpAgent.Api.RateLimit;

[ExcludeFromCodeCoverage]
public static class RateLimitExtensions
{
    public const string CatalogPolicy = "catalog";
    public const string AuthPolicy    = "auth";

    public static IServiceCollection AddCatalogRateLimiter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var opts = configuration
            .GetSection("RateLimit")
            .Get<RateLimitOptions>() ?? new RateLimitOptions();

        services.AddRateLimiter(rl =>
        {
            // ── Catálogo (Fixed Window) ────────────────────────────────────
            rl.AddFixedWindowLimiter(CatalogPolicy, o =>
            {
                o.PermitLimit          = opts.FixedWindow.PermitLimit;
                o.Window               = TimeSpan.FromSeconds(opts.FixedWindow.WindowSeconds);
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit           = opts.FixedWindow.QueueLimit;
            });

            // ── Auth — login / register (Sliding Window por IP) ───────────
            // 5 tentativas por 60 segundos por IP — previne brute force
            rl.AddSlidingWindowLimiter(AuthPolicy, o =>
            {
                o.PermitLimit         = 5;
                o.Window              = TimeSpan.FromSeconds(60);
                o.SegmentsPerWindow   = 6;   // janela dividida em blocos de 10s
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit          = 0;   // sem fila — rejeita imediatamente
            });

            rl.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Informa ao cliente quando pode tentar novamente e registra o evento
            rl.OnRejected = (ctx, _) =>
            {
                var retrySeconds = ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? (long)retryAfter.TotalSeconds
                    : 60L;

                ctx.HttpContext.Response.Headers.RetryAfter =
                    retrySeconds.ToString(CultureInfo.InvariantCulture);

                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger(nameof(RateLimitExtensions));

                logger.LogWarning(
                    "Rate limit excedido — policy={Policy} path={Path} ip={Ip} retryAfter={RetryAfter}s",
                    ctx.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IRateLimiterMetadata>() is { } ? AuthPolicy : CatalogPolicy,
                    ctx.HttpContext.Request.Path,
                    ctx.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "-",
                    retrySeconds);

                return ValueTask.CompletedTask;
            };
        });

        return services;
    }
}
