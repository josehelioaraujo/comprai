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
            // 5 tentativas por 60 segundos — previne brute force
            rl.AddSlidingWindowLimiter(AuthPolicy, o =>
            {
                o.PermitLimit          = 5;
                o.Window               = TimeSpan.FromSeconds(60);
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
