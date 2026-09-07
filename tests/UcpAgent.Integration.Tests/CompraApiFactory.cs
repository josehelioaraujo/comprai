using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using UcpAgent.Application.Search;
using UcpAgent.Application.Cart;
using UcpAgent.Application.Checkout;
using UcpAgent.Application.Orders;
using UcpAgent.SharedKernel;

namespace UcpAgent.Integration.Tests;

public class CompraApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTest");

        builder.ConfigureAppConfiguration((ctx, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:UsarMockDados"]   = "false",
                ["Features:UsarRedis"]       = "false",
                ["Features:UsarKafka"]       = "false",
                ["Features:UsarRabbitMQ"]    = "false",
                ["Redis:ConnectionString"]   = "localhost:6399",
                ["MercadoLivre:AccessToken"] = Environment.GetEnvironmentVariable("ML_ACCESS_TOKEN") ?? ""
            });
        });

        builder.ConfigureServices(services =>
        {
            services.AddScoped<ISearchService,   MockSearchService>();
            services.AddScoped<ICartService,     MockCartService>();
            services.AddScoped<ICheckoutService, MockCheckoutService>();
            services.AddScoped<IOrderService,    MockOrderService>();
        });
    }

    public HttpClient CreateClientNoRedirect() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private sealed class MockSearchService : ISearchService
    {
        public Task<Result<IReadOnlyList<object>>> SearchAsync(string query, int limit = 10, CancellationToken ct = default)
        {
            IReadOnlyList<object> list = new List<object> { new { id = "MLB123", name = "Notebook Teste", price = 1999.99 } };
            return Task.FromResult(Result<IReadOnlyList<object>>.Ok(list));
        }
    }

    private sealed class MockCartService : ICartService
    {
        public Task<Result<object>> GetCartAsync(string sessionId, CancellationToken ct = default)
            => Task.FromResult(Result<object>.Ok((object)new { sessionId, items = Array.Empty<object>() }));
        public Task<Result<object>> AddItemAsync(string sessionId, string productId, int quantity, CancellationToken ct = default)
            => Task.FromResult(Result<object>.Ok((object)new { itemId = Guid.NewGuid().ToString() }));
        public Task<Result<object>> RemoveItemAsync(string sessionId, string productId, CancellationToken ct = default)
            => Task.FromResult(Result<object>.Ok((object)true));
    }

    private sealed class MockCheckoutService : ICheckoutService
    {
        public Task<Result<object>> CheckoutAsync(string sessionId, CancellationToken ct = default)
        {
            var prefix = sessionId.Length >= 6 ? sessionId[..6].ToUpper() : sessionId.ToUpper();
            return Task.FromResult(Result<object>.Ok((object)new { orderId = $"ORDER-{prefix}" }));
        }
    }

    private sealed class MockOrderService : IOrderService
    {
        public Task<Result<object>> GetByIdAsync(string orderId, CancellationToken ct = default)
            => Task.FromResult(Result<object>.Ok((object)new { orderId, status = "created" }));
        public Task<Result<object>> GetBySessionAsync(string sessionId, CancellationToken ct = default)
            => Task.FromResult(Result<object>.Ok((object)new { sessionId, orders = Array.Empty<object>() }));
    }
}
