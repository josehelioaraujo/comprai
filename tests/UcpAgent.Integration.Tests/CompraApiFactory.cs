using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using UcpAgent.Application.Search;
using UcpAgent.Application.Cart;
using UcpAgent.Application.Checkout;
using UcpAgent.Application.Orders;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

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
        public Task<Result<SearchResult>> SearchAsync(string query, int limit = 10, CancellationToken ct = default)
        {
            var product = new ProductDto("MLB123", "Notebook Teste", 1999.99m, null, null, null, "MercadoLivre");
            var result = new SearchResult(new List<ProductDto> { product }, 1, 1, limit, "mock");
            return Task.FromResult(Result<SearchResult>.Ok(result));
        }
    }

    private sealed class MockCartService : ICartService
    {
        public Task<Result<IReadOnlyList<CartItemDto>>> GetCartAsync(string sessionId, CancellationToken ct = default)
        {
            IReadOnlyList<CartItemDto> items = new List<CartItemDto>();
            return Task.FromResult(Result<IReadOnlyList<CartItemDto>>.Ok(items));
        }

        public Task<Result<string>> AddItemAsync(string sessionId, ProductDto product, int quantity = 1, CancellationToken ct = default)
            => Task.FromResult(Result<string>.Ok(Guid.NewGuid().ToString()));

        public Task<Result<bool>> RemoveItemAsync(string sessionId, string productId, CancellationToken ct = default)
            => Task.FromResult(Result<bool>.Ok(true));
    }

    private sealed class MockCheckoutService : ICheckoutService
    {
        public Task<Result<CheckoutResultDto>> CheckoutAsync(string sessionId, CustomerDto customer, CancellationToken ct = default)
        {
            var prefix = sessionId.Length >= 6 ? sessionId[..6].ToUpper() : sessionId.ToUpper();
            return Task.FromResult(Result<CheckoutResultDto>.Ok(new CheckoutResultDto($"ORDER-{prefix}", true, null)));
        }
    }

    private sealed class MockOrderService : IOrderService
    {
        public Task<Result<OrderStatusDto?>> GetByIdAsync(string orderId, CancellationToken ct = default)
            => Task.FromResult(Result<OrderStatusDto?>.Ok(null));

        public Task<Result<OrderStatusDto?>> GetBySessionAsync(string sessionId, CancellationToken ct = default)
            => Task.FromResult(Result<OrderStatusDto?>.Ok(null));
    }
}