using Microsoft.AspNetCore.RateLimiting;
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
        });

        return services;
    }
}
