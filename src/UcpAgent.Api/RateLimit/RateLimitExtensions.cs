using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Threading.RateLimiting;
using System.Diagnostics.CodeAnalysis;

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
        services.AddRateLimiter(rl =>
        {
            // Lê a configuração AQUI (dentro do lambda) e não antes do AddRateLimiter.
            // O lambda é armazenado como IConfigureOptions<RateLimiterOptions> e executa
            // LAZILY quando IOptions<RateLimiterOptions>.Value é acessado na 1ª request —
            // após todos os ConfigureAppConfiguration callbacks (inclusive do test factory)
            // já terem sido processados, garantindo que overrides como PermitLimit=2 sejam lidos.
            var opts = configuration
                .GetSection("RateLimit")
                .Get<RateLimitOptions>() ?? new RateLimitOptions();

            // ── Catálogo (Fixed Window) ────────────────────────────────────
            rl.AddFixedWindowLimiter(CatalogPolicy, o =>
            {
                o.PermitLimit          = opts.FixedWindow.PermitLimit;
                o.Window               = TimeSpan.FromSeconds(opts.FixedWindow.WindowSeconds);
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit           = opts.FixedWindow.QueueLimit;
            });

            // ── Auth — login / register (Sliding Window por IP) ───────────
            // Padrão: 5 / 60 s — previne brute force.
            // Ajuste via env: RateLimit__Auth__PermitLimit=30 (E2E/dev)
            rl.AddSlidingWindowLimiter(AuthPolicy, o =>
            {
                o.PermitLimit          = opts.Auth.PermitLimit;
                o.Window               = TimeSpan.FromSeconds(opts.Auth.WindowSeconds);
                o.SegmentsPerWindow    = 6;
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit           = 0;
            });

            rl.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

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
                    "Rate limit excedido — path={Path} ip={Ip} retryAfter={RetryAfter}s",
                    ctx.HttpContext.Request.Path,
                    ctx.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "-",
                    retrySeconds);

                return ValueTask.CompletedTask;
            };
        });

        return services;
    }
}
