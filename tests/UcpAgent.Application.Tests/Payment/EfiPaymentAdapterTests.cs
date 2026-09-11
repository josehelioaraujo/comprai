using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UcpAgent.Api.Adapters;
using UcpAgent.SharedKernel.Ports;
using Xunit;

namespace UcpAgent.Application.Tests.Payment;

public sealed class EfiPaymentAdapterTests
{
    // ── Setup helpers ────────────────────────────────────────────────────────

    private static EfiPayOptions SandboxOpts() => new()
    {
        ClientId     = "test-id",
        ClientSecret = "test-secret",
        ChavePix     = "chave@test.com",
        Sandbox      = true
    };

    private static IMemoryCache BuildCache()
    {
        var svc = new ServiceCollection();
        svc.AddMemoryCache();
        return svc.BuildServiceProvider().GetRequiredService<IMemoryCache>();
    }

    private static HttpResponseMessage OkJson(string json) =>
        new(HttpStatusCode.OK)
            { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    // Handler que roteia por prefixo de path
    private sealed class RoutingHandler(Dictionary<string, HttpResponseMessage> routes)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage req, CancellationToken _)
        {
            var path = req.RequestUri!.PathAndQuery;
            foreach (var kv in routes)
                if (path.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
                    return Task.FromResult(kv.Value);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    // Handler que conta quantas vezes /oauth/token foi chamado
    private sealed class CountingHandler(
        string tokenJson, string cobJson, string qrJson) : HttpMessageHandler
    {
        public int TokenCallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage req, CancellationToken _)
        {
            var path = req.RequestUri!.PathAndQuery;
            if (path.StartsWith("/oauth/token", StringComparison.OrdinalIgnoreCase))
            {
                TokenCallCount++;
                return Task.FromResult(OkJson(tokenJson));
            }
            if (path.StartsWith("/v2/cob/", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(OkJson(cobJson));
            if (path.StartsWith("/v2/loc/", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(OkJson(qrJson));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage OkJson(string json) =>
            new(HttpStatusCode.OK)
                { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    private static IHttpClientFactory BuildFactory(HttpMessageHandler handler) =>
        Mock.Of<IHttpClientFactory>(f =>
            f.CreateClient("efipay-pix") ==
            new HttpClient(handler)
                { BaseAddress = new Uri("https://pix-h.api.efipay.com.br") });

    // ── EfiPayOptions.BaseUrl ────────────────────────────────────────────────

    [Fact]
    public void BaseUrl_QuandoSandbox_RetornaSandboxUrl()
    {
        var opts = new EfiPayOptions { Sandbox = true };
        Assert.Equal("https://pix-h.api.efipay.com.br", opts.BaseUrl);
    }

    [Fact]
    public void BaseUrl_QuandoProducao_RetornaProducaoUrl()
    {
        var opts = new EfiPayOptions { Sandbox = false };
        Assert.Equal("https://pix.api.efipay.com.br", opts.BaseUrl);
    }

    // ── Provider inválido ────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_ProviderDiferente_RetornaUnsupported()
    {
        var adapter = new EfiPaymentAdapter(
            SandboxOpts(),
            Mock.Of<IHttpClientFactory>(),
            BuildCache(),
            NullLogger<EfiPaymentAdapter>.Instance);

        var result = await adapter.ProcessAsync(
            "ORDER-001", 10m, "BRL",
            new PaymentMethodDto("stripe", null, null));

        Assert.False(result.Success);
        Assert.Equal("unsupported", result.Status);
        Assert.Contains("stripe", result.Error);
    }

    // ── Sucesso completo: cob + qrcode ───────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_SucessoCompleto_RetornaPixCopiaECola()
    {
        var tokenJson = JsonSerializer.Serialize(new
            { access_token = "tok_abc", token_type = "Bearer", expires_in = 3600 });
        var cobJson = JsonSerializer.Serialize(new
            { txid = "compraiorderid1234567890123", status = "ATIVA", loc = new { id = 42 } });
        var qrJson = JsonSerializer.Serialize(new
        {
            pixCopiaECola = "00020126580014BR.GOV.BCB.PIX",
            imagemQrcode  = "data:image/png;base64,abc123"
        });

        var handler = new RoutingHandler(new Dictionary<string, HttpResponseMessage>
        {
            ["/oauth/token"]       = OkJson(tokenJson),
            ["/v2/cob/"]          = OkJson(cobJson),
            ["/v2/loc/42/qrcode"] = OkJson(qrJson),
        });

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("efipay-pix"))
            .Returns(() => new HttpClient(handler)
                { BaseAddress = new Uri("https://pix-h.api.efipay.com.br") });

        var adapter = new EfiPaymentAdapter(
            SandboxOpts(), factoryMock.Object, BuildCache(),
            NullLogger<EfiPaymentAdapter>.Instance);

        var result = await adapter.ProcessAsync(
            "orderid1234567890123", 5m, "BRL",
            new PaymentMethodDto("efipay", null, null));

        Assert.True(result.Success);
        Assert.Equal("ATIVA", result.Status);
        Assert.Equal("00020126580014BR.GOV.BCB.PIX", result.PixCopiaECola);
        Assert.Equal("data:image/png;base64,abc123", result.PixQrCode);
    }

    // ── Falha na API (cob retorna 400) ───────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_FalhaNaCob_RetornaErro()
    {
        var tokenJson = JsonSerializer.Serialize(new
            { access_token = "tok", token_type = "Bearer", expires_in = 3600 });
        var errorBody = "{\"titulo\":\"Valor inválido\",\"status\":400}";

        var handler = new RoutingHandler(new Dictionary<string, HttpResponseMessage>
        {
            ["/oauth/token"] = OkJson(tokenJson),
            ["/v2/cob/"]    = new HttpResponseMessage(HttpStatusCode.BadRequest)
                { Content = new StringContent(errorBody) },
        });

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("efipay-pix"))
            .Returns(() => new HttpClient(handler)
                { BaseAddress = new Uri("https://pix-h.api.efipay.com.br") });

        var adapter = new EfiPaymentAdapter(
            SandboxOpts(), factoryMock.Object, BuildCache(),
            NullLogger<EfiPaymentAdapter>.Instance);

        var result = await adapter.ProcessAsync(
            "ORDER-FAIL", 5m, "BRL",
            new PaymentMethodDto("efipay", null, null));

        Assert.False(result.Success);
        Assert.Equal("BadRequest", result.Status);
        Assert.NotNull(result.Error);
    }

    // ── Token cacheado ───────────────────────────────────────────────────────

    [Fact]
    public async Task ProcessAsync_SegundaChamada_NaoRequisitaNovoToken()
    {
        var tokenJson = JsonSerializer.Serialize(new
            { access_token = "tok", token_type = "Bearer", expires_in = 3600 });
        var cobJson = JsonSerializer.Serialize(new
            { txid = "compraiorderid1234567890123", status = "ATIVA", loc = new { id = 1 } });
        var qrJson = JsonSerializer.Serialize(new
            { pixCopiaECola = "pix123", imagemQrcode = "img" });

        var handler = new CountingHandler(tokenJson, cobJson, qrJson);

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("efipay-pix"))
            .Returns(() => new HttpClient(handler)
                { BaseAddress = new Uri("https://pix-h.api.efipay.com.br") });

        var adapter = new EfiPaymentAdapter(
            SandboxOpts(), factoryMock.Object, BuildCache(),
            NullLogger<EfiPaymentAdapter>.Instance);

        var method = new PaymentMethodDto("efipay", null, null);
        await adapter.ProcessAsync("orderid1234567890123", 5m, "BRL", method);
        await adapter.ProcessAsync("orderid1234567890123", 5m, "BRL", method);

        // Token deve ter sido requisitado apenas uma vez (segunda chamada usa cache)
        Assert.Equal(1, handler.TokenCallCount);
    }
}
