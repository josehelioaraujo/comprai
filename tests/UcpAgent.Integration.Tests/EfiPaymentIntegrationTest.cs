using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UcpAgent.Api.Adapters;
using UcpAgent.SharedKernel.Ports;
using Xunit;

namespace UcpAgent.Integration.Tests;

public sealed class EfiPaymentIntegrationTest
{
    // Factory dedicada: ativa provider "efipay" com mock DI, sem credenciais reais
    private sealed class EfiPayFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("IntegrationTest");

            builder.ConfigureAppConfiguration((_, cfg) =>
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    // usa mock para catálogo — evita dependências externas em CI
                    ["Features:UsarMockDados"]   = "true",
                    ["Features:PaymentProvider"] = "efipay",
                    ["EfiPay:ClientId"]          = "ci-id",
                    ["EfiPay:ClientSecret"]      = "ci-secret",
                    ["EfiPay:ChavePix"]          = "ci@pix.com",
                    ["EfiPay:Sandbox"]           = "true"
                }));
        }
    }

    [Fact]
    public void AddEfiPayment_QuandoProviderEhEfipay_RegistraEfiPaymentAdapter()
    {
        using var factory = new EfiPayFactory();

        // Acionar a construção do host
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
