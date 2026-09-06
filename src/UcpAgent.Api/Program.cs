using UcpAgent.Infrastructure.Messaging;
using Microsoft.Extensions.Caching.Hybrid;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using UcpAgent.Application.Search;
using UcpAgent.Application.Cart;
using UcpAgent.Application.Checkout;
using UcpAgent.Application.Order;
using UcpAgent.Api.Mocks;
using UcpAgent.SharedKernel.Ports;
using UcpAgent.Catalog.MercadoLivre;
using UcpAgent.Catalog.VtexCatalog;
using UcpAgent.Catalog.VtexSearch;
using UcpAgent.Catalog.OpenFoodFacts;
using UcpAgent.Infrastructure.Cart;
using UcpAgent.Infrastructure.Checkout;
using UcpAgent.Infrastructure.Orders;

var builder = WebApplication.CreateBuilder(args);

// ── MediatR ──────────────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<SearchProductsHandler>());

// ── HybridCache + Redis ───────────────────────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(opt =>
    opt.Configuration = builder.Configuration["Redis:ConnectionString"]);

builder.Services.AddHybridCache(opt =>
{
    opt.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration           = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };
});

// ── OpenTelemetry ─────────────────────────────────────────────────────────────
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("comprai-api"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

// ── OpenAPI ───────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── Ports / Adapters ──────────────────────────────────────────────────────────
var usarMock    = builder.Configuration.GetValue<bool>("Features:UsarMockDados");
var usarRedis   = builder.Configuration.GetValue<bool>("Features:UsarRedis");
var usarKafka   = builder.Configuration.GetValue<bool>("Features:UsarKafka");
var usarRabbitMq = builder.Configuration.GetValue<bool>("Features:UsarRabbitMQ");

if (usarMock)
{
    builder.Services.AddSingleton<IProductCatalogPort, MockCatalogPlugin>();
    builder.Services.AddSingleton<ICartPort, InMemoryCartPort>();
    builder.Services.AddSingleton<ICheckoutPort, MockCheckoutPort>();
    builder.Services.AddSingleton<IOrderPort, MockOrderPort>();
}
else
{
    // Plugins de catálogo
    builder.Services.AddHttpClient<MercadoLivrePlugin>();
    builder.Services.AddSingleton<IProductCatalogPort, MercadoLivrePlugin>();
    builder.Services.AddHttpClient<VtexCatalogPlugin>();
    builder.Services.AddSingleton<IProductCatalogPort, VtexCatalogPlugin>();
    builder.Services.AddHttpClient<VtexSearchPlugin>();
    builder.Services.AddSingleton<IProductCatalogPort, VtexSearchPlugin>();
    builder.Services.AddHttpClient<OpenFoodFactsPlugin>();
    builder.Services.AddSingleton<IProductCatalogPort, OpenFoodFactsPlugin>();

    if (usarRedis)
    {
        // Conexão Redis compartilhada
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));

        // Cart, Checkout e Order via Redis
        builder.Services.AddSingleton<ICartPort, RedisCartAdapter>();
        builder.Services.AddSingleton<RedisOrderAdapter>();
        builder.Services.AddSingleton<IOrderPort>(sp => sp.GetRequiredService<RedisOrderAdapter>());
        builder.Services.AddSingleton<ICheckoutPort, RedisCheckoutAdapter>();
    }
    else
    {
        builder.Services.AddSingleton<ICartPort, InMemoryCartPort>();
        builder.Services.AddSingleton<ICheckoutPort, MockCheckoutPort>();
        builder.Services.AddSingleton<IOrderPort, MockOrderPort>();
    }
}

// ── IEventPublisher ───────────────────────────────────────────────────────────
if (usarKafka)
{
    var bootstrapServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
    builder.Services.AddSingleton<IEventPublisher>(_ => new KafkaEventPublisher(bootstrapServers));
}
else if (usarRabbitMq)
{
    var host     = builder.Configuration["RabbitMq:Host"]     ?? "localhost";
    var user     = builder.Configuration["RabbitMq:UserName"] ?? "guest";
    var password = builder.Configuration["RabbitMq:Password"] ?? "guest";
    builder.Services.AddSingleton<IEventPublisher>(_ =>
        new RabbitMqEventPublisher(host, user, password));
}
else
{
    builder.Services.AddSingleton<IEventPublisher, NullEventPublisher>();
}

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

// ── Health ────────────────────────────────────────────────────────────────────
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }))
   .WithTags("Health");

app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }))
   .WithTags("Health");

// ── Search (cache 5 min) ──────────────────────────────────────────────────────
app.MapGet("/api/search", async (
    string q, int page, int pageSize,
    string? category, decimal? minPrice, decimal? maxPrice,
    IMediator mediator, HybridCache cache, CancellationToken ct) =>
{
    var cacheKey = $"search:{q}:{page}:{pageSize}:{category}:{minPrice}:{maxPrice}";
    var result = await cache.GetOrCreateAsync(
        cacheKey,
        async token => await mediator.Send(
            new SearchProductsQuery(q, page, pageSize, category, minPrice, maxPrice), token),
        cancellationToken: ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error);
})
.WithTags("Search").WithName("SearchProducts");

// ── Cart ──────────────────────────────────────────────────────────────────────
app.MapGet("/api/cart/{sessionId}", async (
    string sessionId, ICartPort cart, CancellationToken ct) =>
{
    var items = await cart.GetItemsAsync(sessionId, ct);
    return Results.Ok(new { sessionId, items, total = items.Sum(i => i.Subtotal) });
})
.WithTags("Cart").WithName("GetCart");

app.MapPost("/api/cart/{sessionId}/items", async (
    string sessionId, AddToCartRequest req,
    IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new AddToCartCommand(sessionId, req.Product, req.Quantity), ct);
    return result.IsSuccess ? Results.Ok(new { itemId = result.Value }) : Results.Problem(result.Error);
})
.WithTags("Cart").WithName("AddToCart");

app.MapDelete("/api/cart/{sessionId}/items/{itemId}", async (
    string sessionId, string itemId, ICartPort cart, CancellationToken ct) =>
{
    await cart.RemoveItemAsync(sessionId, itemId, ct);
    return Results.NoContent();
})
.WithTags("Cart").WithName("RemoveCartItem");

// ── Checkout ──────────────────────────────────────────────────────────────────
app.MapPost("/api/checkout/{sessionId}", async (
    string sessionId, CustomerDto customer,
    IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new CheckoutCommand(sessionId, customer), ct);
    if (!result.IsSuccess) return Results.Problem(result.Error);
    return result.Value.Success
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { error = result.Value.Error });
})
.WithTags("Checkout").WithName("Checkout");

// ── Order ─────────────────────────────────────────────────────────────────────
app.MapGet("/api/orders/{orderId}", async (
    string orderId, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new GetOrderQuery(orderId), ct);
    return result.IsSuccess
        ? (result.Value is null ? Results.NotFound() : Results.Ok(result.Value))
        : Results.Problem(result.Error);
})
.WithTags("Order").WithName("GetOrder");

app.Run();

// ── Request DTOs ──────────────────────────────────────────────────────────────
record AddToCartRequest(UcpAgent.SharedKernel.Models.ProductDto Product, int Quantity = 1);
