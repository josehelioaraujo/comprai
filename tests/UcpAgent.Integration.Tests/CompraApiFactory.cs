using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;

namespace UcpAgent.Integration.Tests;

public class CompraApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");

        builder.ConfigureServices(services =>
        {
            // Feature flags: ML real, Redis mock, Kafka off, RabbitMQ off
            Environment.SetEnvironmentVariable("UsarMockDados", "false");
            Environment.SetEnvironmentVariable("UsarRedis", "false");
            Environment.SetEnvironmentVariable("UsarKafka", "false");
            Environment.SetEnvironmentVariable("UsarRabbitMQ", "false");

            // Token ML real via env var
            var mlToken = Environment.GetEnvironmentVariable("ML_ACCESS_TOKEN") ?? "";
            Environment.SetEnvironmentVariable("MercadoLivre__AccessToken", mlToken);

            // Token Shopify real via env var
            var shopifyToken = Environment.GetEnvironmentVariable("SHOPIFY_ACCESS_TOKEN") ?? "";
            Environment.SetEnvironmentVariable("Shopify__AccessToken", shopifyToken);
        });
    }

    public HttpClient CreateClientNoRedirect()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
}
