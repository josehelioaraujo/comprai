using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace UcpAgent.Integration.Tests;

/// <summary>
/// Prova que o ASP.NET RateLimiter devolve HTTP 429 após esgotar o PermitLimit.
/// Usa WebApplicationFactory própria (não IClassFixture) para isolar o estado do limiter.
/// </summary>
public sealed class RateLimiterIntegrationTest
{
    [Fact]
    public async Task Search_AoExcederPermitLimit_Retorna429()
    {
        // Arrange – factory com PermitLimit=2 para forçar o bloqueio rapidamente
        await using var factory = new RateLimitTestFactory();
        var client = factory.CreateClient();

        // Act – 2 requisições permitidas + 1 bloqueada pelo rate limiter
        var r1 = await client.GetAsync("/api/search?q=a&page=1&pageSize=1");
        var r2 = await client.GetAsync("/api/search?q=b&page=1&pageSize=1");
        var r3 = await client.GetAsync("/api/search?q=c&page=1&pageSize=1");

        // Assert – terceira requisição deve ser rejeitada com 429
        Assert.NotEqual(HttpStatusCode.TooManyRequests, r1.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, r2.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, r3.StatusCode);
    }
}

/// <summary>
/// Factory que sobrepõe PermitLimit=2 para que o teste possa
/// forçar um HTTP 429 com apenas 3 requisições consecutivas.
/// </summary>
internal sealed class RateLimitTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimit:FixedWindow:PermitLimit"]   = "2",
                ["RateLimit:FixedWindow:WindowSeconds"] = "60",
                ["RateLimit:FixedWindow:QueueLimit"]    = "0",
                // Infra mínima necessária para o teste não falhar na inicialização
                ["Features:UsarRedis"]     = "false",
                ["Features:UsarKafka"]     = "false",
                ["Features:UsarRabbitMQ"]  = "false",
                ["Features:UsarMockDados"] = "false",
            });
        });
    }
}
