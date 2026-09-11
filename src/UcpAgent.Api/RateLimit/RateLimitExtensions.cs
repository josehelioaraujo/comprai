using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Threading.RateLimiting;

namespace UcpAgent.Api.RateLimit;

public static class RateLimitExtensions
{
    public const string CatalogPolicy = "catalog";

    public static IServiceCollection AddCatalogRateLimiter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var opts = configuration
            .GetSection("RateLimit")
            .Get<RateLimitOptions>() ?? new RateLimitOptions();

        services.AddRateLimiter(rl =>
        {
            rl.AddFixedWindowLimiter(CatalogPolicy, o =>
            {
                o.PermitLimit          = opts.FixedWindow.PermitLimit;
                o.Window               = TimeSpan.FromSeconds(opts.FixedWindow.WindowSeconds);
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit           = opts.FixedWindow.QueueLimit;
            });

            rl.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Informa ao cliente quando pode tentar novamente e registra o evento
            rl.OnRejected = (ctx, _) =>
            {
                // Prefere metadado do lease; cai no WindowSeconds como fallback
                var retrySeconds = ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? (long)retryAfter.TotalSeconds
                    : (long)opts.FixedWindow.WindowSeconds;

                ctx.HttpContext.Response.Headers.RetryAfter =
                    retrySeconds.ToString(CultureInfo.InvariantCulture);

                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger(nameof(RateLimitExtensions));

                logger.LogWarning(
                    "Rate limit excedido — policy={Policy} path={Path} retryAfter={RetryAfter}s",
                    CatalogPolicy,
                    ctx.HttpContext.Request.Path,
                    retrySeconds);

                return ValueTask.CompletedTask;
            };
        });

        return services;
    }
}
