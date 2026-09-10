using Microsoft.Extensions.Configuration;
using System.Threading.RateLimiting;
using UcpAgent.Api.RateLimit;
using Xunit;

namespace UcpAgent.Application.Tests.RateLimit;

public class RateLimiterTests
{
    private static IConfiguration BuildConfig(int permitLimit = 10, int windowSeconds = 10, int queueLimit = 0)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimit:FixedWindow:PermitLimit"]   = permitLimit.ToString(),
                ["RateLimit:FixedWindow:WindowSeconds"] = windowSeconds.ToString(),
                ["RateLimit:FixedWindow:QueueLimit"]    = queueLimit.ToString()
            })
            .Build();
    }

    [Fact]
    public void Options_DevemCarregarCorretamenteDaConfig()
    {
        var config = BuildConfig(permitLimit: 5, windowSeconds: 30, queueLimit: 2);
        var opts = config.GetSection("RateLimit").Get<RateLimitOptions>();

        Assert.NotNull(opts);
        Assert.Equal(5,  opts.FixedWindow.PermitLimit);
        Assert.Equal(30, opts.FixedWindow.WindowSeconds);
        Assert.Equal(2,  opts.FixedWindow.QueueLimit);
    }

    [Fact]
    public void Options_DeveUsarValoresPadrao()
    {
        var opts = new RateLimitOptions();
        Assert.Equal(100, opts.FixedWindow.PermitLimit);
        Assert.Equal(10,  opts.FixedWindow.WindowSeconds);
        Assert.Equal(0,   opts.FixedWindow.QueueLimit);
    }

    [Fact]
    public async Task FixedWindow_DevePermitirAteLimite()
    {
        using var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit          = 3,
            Window               = TimeSpan.FromSeconds(60),
            QueueLimit           = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });

        for (int i = 0; i < 3; i++)
        {
            using var lease = await limiter.AcquireAsync();
            Assert.True(lease.IsAcquired, $"Requisicao {i + 1} deveria ser permitida");
        }
    }

    [Fact]
    public async Task FixedWindow_DeveRejeitarAposExcederLimite()
    {
        using var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit          = 3,
            Window               = TimeSpan.FromSeconds(60),
            QueueLimit           = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });

        for (int i = 0; i < 3; i++)
        {
            using var lease = await limiter.AcquireAsync();
        }

        using var rejectedLease = await limiter.AcquireAsync();
        Assert.False(rejectedLease.IsAcquired, "Requisicao alem do limite deveria ser rejeitada (429)");
    }

    [Fact]
    public async Task FixedWindow_NaoDevePermitirFilaQuandoQueueLimitZero()
    {
        using var limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit          = 1,
            Window               = TimeSpan.FromSeconds(60),
            QueueLimit           = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });

        using var first = await limiter.AcquireAsync();
        Assert.True(first.IsAcquired);

        using var second = limiter.AttemptAcquire();
        Assert.False(second.IsAcquired, "Fila zero: segunda requisicao nao deve ser enfileirada");
    }

    [Fact]
    public void CatalogPolicy_NomeCorreto()
    {
        Assert.Equal("catalog", RateLimitExtensions.CatalogPolicy);
    }
}
