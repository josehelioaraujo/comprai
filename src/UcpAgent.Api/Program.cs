using UcpAgent.Catalog.Shopify;
using UcpAgent.Application.Orders;
using UcpAgent.Api.Endpoints;
using UcpAgent.Api;
using Scalar.AspNetCore;
using MediatR;
using UcpAgent.Application.IntentRouter;
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
builder.Services.AddObservabilidade(builder.Configuration);

// â”€â”€ MediatR â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<SearchProductsHandler>());

// â”€â”€ HybridCache + Redis â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

// â”€â”€ OpenTelemetry â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("comprai-api"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

// â”€â”€ OpenAPI â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddOpenApi();

// â”€â”€ Health Checks â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
builder.Services.AddHealthChecks();

// â”€â”€ Ports / Adapters â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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
    // Plugins de catÃ¡logo
    builder.Services.AddHttpClient<MercadoLivrePlugin>();
    builder.Services.AddSingleton<IProductCatalogPort, MercadoLivrePlugin>();
    builder.Services.AddHttpClient<VtexCatalogPlugin>();
    builder.Services.AddSingleton<IProductCatalogPort, VtexCatalogPlugin>();
    builder.Services.AddHttpClient<VtexSearchPlugin>();
    builder.Services.AddSingleton<IProductCatalogPort, VtexSearchPlugin>();
    builder.Services.AddHttpClient<OpenFoodFactsPlugin>();
    builder.Services.Configure<ShopifyOptions>(builder.Configuration.GetSection("Shopify"));
    builder.Services.AddHttpClient<ShopifyPlugin>(c => { c.Timeout = TimeSpan.FromSeconds(15); });
    builder.Services.AddTransient<IProductCatalogPort, ShopifyPlugin>();
    builder.Services.AddSingleton<IProductCatalogPort, OpenFoodFactsPlugin>();

    if (usarRedis)
    {
        // ConexÃ£o Redis compartilhada
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

// â”€â”€ IEventPublisher â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

builder.Services.AddSingleton<IIntentRouterService, IntentRouterService>();

//  MercadoLivre OAuth + Orders 
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("MlAuth");
builder.Services.AddHttpClient("MlApi");
builder.Services.AddSingleton<UcpAgent.Catalog.MercadoLivreOrders.MlTokenService>();
builder.Services.AddSingleton<UcpAgent.Catalog.MercadoLivreOrders.MlOrdersService>();
builder.Services.AddSingleton<UcpAgent.Catalog.MercadoLivreOrders.MlWebhookService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<IOrderService, OrderService>();
var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

// â”€â”€ Health â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }))
   .WithTags("Health");

app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }))
   .WithTags("Health");

// â”€â”€ Search (cache 5 min) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.MapGet("/api/search", async (
    string q, int page, int pageSize,
    string? category, decimal? minPrice, decimal? maxPrice,
    IMediator mediator, HybridCache cache, CancellationToken ct) =>
{
    var cacheKey = $"search:{q}:{page}:{pageSize}:{category}:{minPrice}:{maxPrice}";
    Result<SearchResult>? result = null;
    try
    {
        result = await cache.GetOrCreateAsync(
            cacheKey,
            async token =>
            {
                var r = await mediator.Send(
                    new SearchProductsQuery(q, page, pageSize, category, minPrice, maxPrice), token);
                // Nao cacheia resultado vazio — evita que falha temporaria de plugin fique cacheada
                if (r.IsSuccess && r.Value.Items.Count == 0)
                    throw new InvalidOperationException("__skip_cache__");
                return r;
            },
            cancellationToken: ct);
    }
    catch (InvalidOperationException ex) when (ex.Message == "__skip_cache__")
    {
        // Resultado vazio: executa sem cache
        result = await mediator.Send(
            new SearchProductsQuery(q, page, pageSize, category, minPrice, maxPrice), ct);
    }
    return result!.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error);
})
.WithTags("Search").WithName("SearchProducts");

// â”€â”€ Cart â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

// â”€â”€ Checkout â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

// â”€â”€ Order â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
app.MapGet("/api/orders/{orderId}", async (
    string orderId, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new GetOrderQuery(orderId), ct);
    return result.IsSuccess
        ? (result.Value is null ? Results.NotFound() : Results.Ok(result.Value))
        : Results.Problem(result.Error);
})
.WithTags("Order").WithName("GetOrder");


//  ML Auth 
app.MapGet("/api/ml/auth", (UcpAgent.Catalog.MercadoLivreOrders.MlTokenService ml) =>
    Results.Redirect(ml.GetAuthorizationUrl()))
   .WithTags("MercadoLivre");

app.MapGet("/callback", async (string code, UcpAgent.Catalog.MercadoLivreOrders.MlTokenService ml, CancellationToken ct) => {
    var token = await ml.ExchangeCodeAsync(code, ct);
    return Results.Ok(new { message = "Autenticado com sucesso!", userId = token.UserId });
}).WithTags("MercadoLivre");

//  ML Orders 
app.MapGet("/api/ml/orders", async (
    string? status, int limit, int offset,
    UcpAgent.Catalog.MercadoLivreOrders.MlOrdersService orders, CancellationToken ct) => {
    var result = await orders.GetOrdersAsync(status, limit == 0 ? 20 : limit, offset, ct);
    return Results.Ok(new { data = result, total = result.Count });
}).WithTags("MercadoLivre");

app.MapGet("/api/ml/orders/{orderId}", async (
    string orderId,
    UcpAgent.Catalog.MercadoLivreOrders.MlOrdersService orders, CancellationToken ct) => {
    var order = await orders.GetOrderAsync(orderId, ct);
    return order is null ? Results.NotFound() : Results.Ok(order);
}).WithTags("MercadoLivre");

//  ML Webhook 
app.MapPost("/webhook/ml", async (
    UcpAgent.Catalog.MercadoLivreOrders.MlWebhookPayload payload,
    UcpAgent.Catalog.MercadoLivreOrders.MlWebhookService webhook, CancellationToken ct) => {
    var result = await webhook.ProcessAsync(payload, ct);
    return Results.Ok(result);
}).WithTags("MercadoLivre");
app.MapIntentEndpoints();
app.MapGet("/api/ml/token-debug", async (UcpAgent.Catalog.MercadoLivreOrders.MlTokenService ml, CancellationToken ct) => { try { var t = await ml.GetAccessTokenAsync(ct); return Results.Ok(new { access_token = t }); } catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); } }).WithTags("Debug");
app.Run();

// â”€â”€ Request DTOs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
record AddToCartRequest(UcpAgent.SharedKernel.Models.ProductDto Product, int Quantity = 1);








public partial class Program { }
