using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Xunit;

namespace UcpAgent.Integration.Tests;

/// <summary>
/// Prova que o pipeline Polly (Timeout → Retry → Circuit Breaker) funciona de verdade
/// no pipeline ASP.NET, injetando um handler primário que falha nas 2 primeiras chamadas
/// e sucede na terceira — exatamente o comportamento de retry exponencial.
/// </summary>
public sealed class PollyResilienceIntegrationTest
{
    [Fact]
    public async Task Search_ComPollyRetry_ReintentaApos500_ERetorna200()
    {
        // Arrange – stub que falha 2x e sucede na 3ª chamada HTTP
        var stub = new FailTwiceHandler();

        await using var factory = new CompraApiFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // Delay curto para o teste não demorar (padrão é 1 s)
                    ["Resilience:Retry:BaseDelaySeconds"] = "0.05",
                    ["Resilience:Retry:MaxAttempts"]      = "3",
                });
            });

            builder.ConfigureTestServices(services =>
            {
                // Injeta o stub como primary handler do HttpClient de DummyJsonPlugin.
                // O pipeline Polly (já registrado como AdditionalHandler via AddCatalogResilience)
                // envolve o stub, formando a cadeia:  Polly → FailTwiceHandler
                services.Configure<HttpClientFactoryOptions>("DummyJsonPlugin", opts =>
                {
                    opts.HttpMessageHandlerBuilderActions.Add(b =>
                        b.PrimaryHandler = stub);
                });
            });
        });

        var client = factory.CreateClient();

        // Act – uma única chamada ao endpoint; Polly cuida das retentativas internamente
        var response = await client.GetAsync("/api/search?q=notebook&page=1&pageSize=5");

        // Assert – endpoint deve responder 200 após Polly ter reintentado com sucesso
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
