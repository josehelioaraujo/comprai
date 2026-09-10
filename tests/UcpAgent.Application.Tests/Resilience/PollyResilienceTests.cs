using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using System.Net;
using UcpAgent.Api.Resilience;
using Xunit;

namespace UcpAgent.Application.Tests.Resilience;

public class PollyResilienceTests
{
    private static IConfiguration BuildConfig(
        int maxAttempts = 2, double baseDelay = 0.01,
        double failureRatio = 0.5, int minThroughput = 3,
        int samplingSeconds = 5, int breakSeconds = 60, int timeoutSeconds = 2)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Resilience:Retry:MaxAttempts"]                      = maxAttempts.ToString(),
            ["Resilience:Retry:BaseDelaySeconds"]                 = baseDelay.ToString(),
            ["Resilience:CircuitBreaker:FailureRatio"]            = failureRatio.ToString(),
            ["Resilience:CircuitBreaker:MinimumThroughput"]       = minThroughput.ToString(),
            ["Resilience:CircuitBreaker:SamplingDurationSeconds"] = samplingSeconds.ToString(),
            ["Resilience:CircuitBreaker:BreakDurationSeconds"]    = breakSeconds.ToString(),
            ["Resilience:Timeout:TimeoutSeconds"]                 = timeoutSeconds.ToString(),
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static HttpClient BuildClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFn,
        IConfiguration? config = null)
    {
        var cfg = config ?? BuildConfig();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(cfg);
        services.AddHttpClient("test")
            .AddCatalogResilience(cfg)
            .ConfigurePrimaryHttpMessageHandler(() => new StubHandler(handlerFn));
        return services.BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient("test");
    }

    [Fact]
    public async Task Retry_DeveTentarNovamenteApos500()
    {
        int calls = 0;
        var client = BuildClient((_, _) =>
        {
            calls++;
            return Task.FromResult(new HttpResponseMessage(
                calls < 3 ? HttpStatusCode.InternalServerError : HttpStatusCode.OK));
        }, BuildConfig(maxAttempts: 2, baseDelay: 0.01));

        var response = await client.GetAsync("http://test/api");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task Retry_DeveTentarNovamenteApos429()
    {
        int calls = 0;
        var client = BuildClient((_, _) =>
        {
            calls++;
            return Task.FromResult(new HttpResponseMessage(
                calls < 2 ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK));
        }, BuildConfig(maxAttempts: 2, baseDelay: 0.01));

        var response = await client.GetAsync("http://test/api");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(calls >= 2);
    }

    [Fact]
    public async Task Retry_NaoDeveRetentarEm200()
    {
        int calls = 0;
        var client = BuildClient((_, _) =>
        {
            calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }, BuildConfig(maxAttempts: 3, baseDelay: 0.01));

        await client.GetAsync("http://test/api");
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CircuitBreaker_DeveAbrirAposMinThroughput()
    {
        var client = BuildClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)),
            BuildConfig(maxAttempts: 0, minThroughput: 3, failureRatio: 0.5,
                        samplingSeconds: 10, breakSeconds: 60));

        int broken = 0;
        for (int i = 0; i < 10; i++)
        {
            try { await client.GetAsync("http://test/api"); }
            catch (Exception ex) when (ex is BrokenCircuitException
                                    || ex.InnerException is BrokenCircuitException)
            { broken++; }
        }
        Assert.True(broken > 0, "Circuit breaker deveria ter aberto");
    }

    [Fact]
    public async Task Timeout_DeveLancarExcecaoQuandoExcedido()
    {
        var client = BuildClient(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, BuildConfig(maxAttempts: 0, timeoutSeconds: 1));

        await Assert.ThrowsAnyAsync<Exception>(() => client.GetAsync("http://test/api"));
    }

    [Fact]
    public async Task Pipeline_DevePassarRequisicaoBemsucedida()
    {
        var client = BuildClient((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var response = await client.GetAsync("http://test/api");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

internal sealed class StubHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _fn;
    public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> fn) => _fn = fn;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct) => _fn(req, ct);
}
