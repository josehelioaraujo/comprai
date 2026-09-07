using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;

namespace UcpAgent.Tests.Integration;

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
        });
    }
}
