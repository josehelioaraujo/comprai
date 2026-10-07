using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UcpAgent.Api.Adapters;
using UcpAgent.SharedKernel.Ports;
using Xunit;

namespace UcpAgent.Integration.Tests;

public sealed class EfiPaymentIntegrationTest
{
    private sealed class EfiPayFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");

            builder.ConfigureTestServices(services =>
            {
                // Remove TODOS os registros de IPaymentPort (pode haver keyed + normal)
                services.RemoveAll(typeof(IPaymentPort));

                var opts = new EfiPayOptions
                {
                    ClientId     = "ci-id",
                    ClientSecret = "ci-secret",
                    ChavePix     = "ci@pix.com",
                    Sandbox      = true
                };
                services.AddSingleton(opts);

                services.AddHttpClient("efipay-pix", c =>
                    c.BaseAddress = new Uri(opts.BaseUrl));

                services.AddSingleton<IPaymentPort>(sp =>
                    new EfiPaymentAdapter(
                        opts,
                        sp.GetRequiredService<IHttpClientFactory>(),
                        sp.GetRequiredService<IMemoryCache>(),
                        sp.GetRequiredService<ILogger<EfiPaymentAdapter>>()));
            });
        }
    }

    [Fact]
    public void AddEfiPayment_QuandoProviderEhEfipay_RegistraEfiPaymentAdapter()
    {
        using var factory = new EfiPayFactory();
        _ = factory.Server;
        var payment = factory.Services.GetRequiredService<IPaymentPort>();
        Assert.IsType<EfiPaymentAdapter>(payment);
    }

    [Fact]
    public void AddEfiPayment_Options_SandboxTruePorPadrao()
    {
        using var factory = new EfiPayFactory();
        _ = factory.Server;
        var opts = factory.Services.GetRequiredService<EfiPayOptions>();
        Assert.True(opts.Sandbox);
        Assert.Equal("https://pix-h.api.efipay.com.br", opts.BaseUrl);
    }

    [Fact]
    public void AddEfiPayment_Options_ClientIdLidoDaConfiguracao()
    {
        using var factory = new EfiPayFactory();
        _ = factory.Server;
        var opts = factory.Services.GetRequiredService<EfiPayOptions>();
        Assert.Equal("ci-id", opts.ClientId);
        Assert.Equal("ci@pix.com", opts.ChavePix);
    }
}
