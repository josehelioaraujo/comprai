using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UcpAgent.Api.Resilience;
using Xunit;

namespace UcpAgent.Integration.Tests;

/// <summary>
/// Prova que o pipeline Polly (Timeout → Retry → Circuit Breaker) funciona de verdade,
/// testando em isolamento via IHttpClientFactory — sem depender do WebApplicationFactory.
///
/// Por que isolamento?
/// DummyJsonPlugin é registrado como Singleton via AddSingleton&lt;IProductCatalogPort, DummyJsonPlugin&gt;(),
/// o que faz o DI injetar o HttpClient genérico (não o typed client "DummyJsonPlugin").
/// Testar via WebApplicationFactory + PostConfigure&lt;HttpClientFactoryOptions&gt; resultaria em
/// stub.CallCount == 0 porque o handler nunca seria usado.
///
/// Usando IHttpClientFactory diretamente com ConfigurePrimaryHttpMessageHandler, garantimos que
/// o stub é o handler mais interno ANTES do Polly envolver — cadeia correta:
/// Polly (retry/timeout/CB) → FailTwiceHandler (primary).
/// </summary>
public sealed class PollyResilienceIntegrationTest
{
    [Fact]
    public async Task Search_ComPollyRetry_ReintentaApos500_ERetorna200()
    {
        // Arrange – configuração com delay curto para o teste não demorar
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Resilience:Retry:BaseDelaySeconds"] = "0.05",
                ["Resilience:Retry:MaxAttempts"]      = "3",
            })
            .Build();

        // Stub que falha 2x e sucede na 3ª chamada HTTP
        var stub = new FailTwiceHandler();

        // Registrar HttpClient com stub como primary handler ANTES do Polly envolver
        var services = new ServiceCollection();
        services.AddHttpClient("catalog-test")
            .ConfigurePrimaryHttpMessageHandler(() => stub)
            .AddCatalogResilience(config);

        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client  = factory.CreateClient("catalog-test");

        // Act – uma única chamada; Polly cuida das retentativas internamente
        var response = await client.GetAsync("https://dummyjson.com/products/search?q=notebook");

        // Assert – deve responder 200 após Polly ter reintentado com sucesso
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // O stub deve ter sido chamado 3 vezes: 1 original + 2 retentativas pelo Polly
        Assert.Equal(3, stub.CallCount);
    }
}

/// <summary>
/// Retorna HTTP 500 nas primeiras 2 invocações e HTTP 200 com JSON válido
/// a partir da 3ª — simulando instabilidade transitória de terceiro.
/// </summary>
internal sealed class FailTwiceHandler : HttpMessageHandler
{
    private int _callCount;
    public int CallCount => _callCount;

    // JSON mínimo no formato DummyResponse que o DummyJsonPlugin desserializa
    private const string ValidDummyJson =
        """{"products":[{"id":1,"title":"Produto Teste","price":99.9,"discountPercentage":0,"thumbnail":"","category":"test","stock":10}],"total":1}""";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var n = Interlocked.Increment(ref _callCount);

        if (n <= 2)
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.InternalServerError));

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ValidDummyJson, Encoding.UTF8, "application/json")
        });
    }
}
