using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
/// IMPORTANTE: usa UseSetting em vez de ConfigureAppConfiguration.
/// No .NET 10 minimal API, AddCatalogRateLimiter lê a configuração via
/// builder.Configuration durante o registro de serviços. ConfigureAppConfiguration
/// pode ser executado depois desse ponto, tornando o override tardio.
/// UseSetting injeta os valores no WebHostBuilder antes de Build() / Services,
/// garantindo que o rate limiter leia PermitLimit=2 ao ser configurado.
/// </summary>
internal sealed class RateLimitTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");

        // UseSetting define os valores ANTES do registro dos serviços
        builder.UseSetting("RateLimit:FixedWindow:PermitLimit",   "2");
        builder.UseSetting("RateLimit:FixedWindow:WindowSeconds", "60");
        builder.UseSetting("RateLimit:FixedWindow:QueueLimit",    "0");

        // Infra mínima necessária para a inicialização não falhar
        builder.UseSetting("Features:UsarRedis",     "false");
        builder.UseSetting("Features:UsarKafka",     "false");
        builder.UseSetting("Features:UsarRabbitMQ",  "false");
        builder.UseSetting("Features:UsarMockDados", "false");
    }
}
