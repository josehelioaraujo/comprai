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

    [Fact]
    public async Task Search_QuandoRejeitado_DeveRetornarHeaderRetryAfter()
    {
        // Arrange – factory com PermitLimit=2
        await using var factory = new RateLimitTestFactory();
        var client = factory.CreateClient();

        // Esgota o limite
        await client.GetAsync("/api/search?q=a&page=1&pageSize=1");
        await client.GetAsync("/api/search?q=b&page=1&pageSize=1");

        // Act – requisição rejeitada
        var rejected = await client.GetAsync("/api/search?q=c&page=1&pageSize=1");

        // Assert – 429 com Retry-After numérico válido
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(
            rejected.Headers.Contains("Retry-After"),
            "Resposta 429 deve incluir o header Retry-After");

        var retryAfterValue = rejected.Headers.GetValues("Retry-After").First();
        Assert.True(
            long.TryParse(retryAfterValue, out var seconds),
            $"Retry-After deve ser um inteiro, mas veio '{retryAfterValue}'");
        Assert.True(seconds > 0, "Retry-After deve ser maior que zero");
    }
}

/// <summary>
/// Factory que sobrepõe PermitLimit=2 para que o teste possa
/// forçar um HTTP 429 com apenas 3 requisições consecutivas.
///
/// IMPORTANTE: usa ConfigureAppConfiguration + AddInMemoryCollection.
/// No .NET 10 minimal API, AddCatalogRateLimiter lê a configuração via
/// builder.Configuration durante o registro de serviços. O AddInMemoryCollection
/// adicionado por último tem a maior prioridade entre os providers e é resolvido
/// antes de Build() completar, garantindo que o rate limiter leia PermitLimit=2.
///
/// UsarMockDados=true evita chamadas HTTP reais a catálogos externos
/// (MercadoLivre, Vtex…), que causavam timeout de ~2 min e mascaravam o 429.
/// </summary>
internal sealed class RateLimitTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");

        builder.ConfigureAppConfiguration((_, cfg) =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Rate limiter — PermitLimit=2 força o bloqueio na 3ª requisição
                ["RateLimit:FixedWindow:PermitLimit"]   = "2",
                ["RateLimit:FixedWindow:WindowSeconds"] = "60",
                ["RateLimit:FixedWindow:QueueLimit"]    = "0",

                // Infra mínima: desliga brokers externos e usa dados mock rápidos
                // (sem UsarMockDados=true o handler chamaria APIs externas reais,
                //  levando ~2 min de timeout antes de retornar — e o assert falharia)
                ["Features:UsarRedis"]     = "false",
                ["Features:UsarKafka"]     = "false",
                ["Features:UsarRabbitMQ"]  = "false",
                ["Features:UsarMockDados"] = "true",
            }));
    }
}
