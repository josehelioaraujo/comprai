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
using UcpAgent.FakeCatalog;
using UcpAgent.PriceWatcher;
using UcpAgent.PriceWatcher.Channels;
using UcpAgent.PriceWatcher.Hubs;

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

// ── Price Watcher ──────────────────────────────────────────────────────────────
var usarPriceWatcher = builder.Configuration.GetValue<bool>("Features:UsarPriceWatcher");
if (usarPriceWatcher)
{
    builder.Services.AddSignalR();
    var usarRabbitPw = builder.Configuration.GetValue<bool>("Features:UsarRabbitMQ");
    if (usarRabbitPw)
    {
        var pwHost = builder.Configuration["RabbitMq:Host"]     ?? "localhost";
        var pwUser = builder.Configuration["RabbitMq:UserName"] ?? "guest";
        var pwPass = builder.Configuration["RabbitMq:Password"] ?? "guest";
        builder.Services.AddSingleton<IPriceAlertChannel>(sp =>
            new RabbitMqAlertChannel(pwHost, pwUser, pwPass,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RabbitMqAlertChannel>>()));
    }
    else
        builder.Services.AddSingleton<IPriceAlertChannel, NullAlertChannel>();

    builder.Services.AddSingleton<PriceWatcherService>();
    builder.Services.AddHostedService(sp => sp.GetRequiredService<PriceWatcherService>());
}

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
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

app.UseCors("AllowAll");
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
    // Executa sem cache primeiro para verificar se tem resultados
    var result = await mediator.Send(
        new SearchProductsQuery(q, page, pageSize, category, minPrice, maxPrice), ct);
    // So cacheia se tiver ao menos 1 item — evita cachear falha temporaria de plugin
    if (result.IsSuccess && result.Value.Items.Count > 0)
        await cache.SetAsync(cacheKey, result, cancellationToken: ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error);
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
app.MapFakeCatalogEndpoints();

var pwEnabled = app.Configuration.GetValue<bool>("Features:UsarPriceWatcher");
if (pwEnabled)
{
    app.MapHub<PriceHub>("/hubs/price");
    app.MapPriceWatcherEndpoints();
}
app.MapGet("/api/ml/token-debug", async (UcpAgent.Catalog.MercadoLivreOrders.MlTokenService ml, CancellationToken ct) => { try { var t = await ml.GetAccessTokenAsync(ct); return Results.Ok(new { access_token = t }); } catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); } }).WithTags("Debug");

// ── Price Watcher Test Page ────────────────────────────────────────────────────
var pwHtml = "<!DOCTYPE html>\n<html lang=\"pt-BR\">\n<head>\n  <meta charset=\"UTF-8\">\n  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n  <title>Comprai — Price Watcher Test</title>\n  <script src=\"https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js\"></script>\n  <style>\n    * { box-sizing: border-box; margin: 0; padding: 0; }\n    body { font-family: 'Segoe UI', sans-serif; background: #0f172a; color: #e2e8f0; min-height: 100vh; padding: 24px; }\n    h1 { color: #38bdf8; font-size: 1.5rem; margin-bottom: 4px; }\n    .subtitle { color: #64748b; font-size: 0.85rem; margin-bottom: 24px; }\n    .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; max-width: 1100px; }\n    .card { background: #1e293b; border: 1px solid #334155; border-radius: 12px; padding: 20px; }\n    .card h2 { font-size: 1rem; color: #94a3b8; margin-bottom: 16px; text-transform: uppercase; letter-spacing: .05em; }\n    label { display: block; font-size: 0.8rem; color: #64748b; margin-bottom: 4px; margin-top: 12px; }\n    input { width: 100%; background: #0f172a; border: 1px solid #334155; border-radius: 6px; padding: 8px 10px; color: #e2e8f0; font-size: 0.9rem; }\n    input:focus { outline: none; border-color: #38bdf8; }\n    button { margin-top: 14px; width: 100%; padding: 10px; border: none; border-radius: 6px; font-size: 0.9rem; cursor: pointer; font-weight: 600; transition: opacity .2s; }\n    button:hover { opacity: .85; }\n    .btn-primary { background: #0ea5e9; color: #fff; }\n    .btn-danger  { background: #ef4444; color: #fff; }\n    .btn-info    { background: #6366f1; color: #fff; }\n    .status-bar { display: flex; align-items: center; gap: 8px; margin-bottom: 16px; font-size: 0.85rem; }\n    .dot { width: 8px; height: 8px; border-radius: 50%; background: #ef4444; }\n    .dot.connected { background: #22c55e; animation: pulse 2s infinite; }\n    @keyframes pulse { 0%,100%{opacity:1} 50%{opacity:.4} }\n    .log { background: #0f172a; border: 1px solid #1e293b; border-radius: 8px; padding: 12px; font-family: monospace; font-size: 0.78rem; height: 300px; overflow-y: auto; }\n    .log-entry { padding: 4px 0; border-bottom: 1px solid #1e293b; }\n    .log-entry.alert { color: #22c55e; font-weight: bold; }\n    .log-entry.info  { color: #38bdf8; }\n    .log-entry.error { color: #ef4444; }\n    .log-entry.warn  { color: #f59e0b; }\n    .watch-list { max-height: 300px; overflow-y: auto; }\n    .watch-item { background: #0f172a; border: 1px solid #334155; border-radius: 8px; padding: 12px; margin-bottom: 8px; }\n    .watch-item .title { font-weight: 600; color: #e2e8f0; font-size: 0.9rem; }\n    .watch-item .prices { display: flex; gap: 16px; margin-top: 6px; font-size: 0.8rem; color: #64748b; }\n    .watch-item .prices span b { color: #38bdf8; }\n    .watch-item .remove  { float: right; background: none; border: 1px solid #ef4444; color: #ef4444; border-radius: 4px; padding: 2px 8px; font-size: 0.75rem; cursor: pointer; margin-top: -2px; width: auto; margin-left: 4px; }\n    .watch-item .trigger { float: right; background: none; border: 1px solid #22c55e; color: #22c55e; border-radius: 4px; padding: 2px 8px; font-size: 0.75rem; cursor: pointer; margin-top: -2px; width: auto; }\n    .alert-banner { background: #052e16; border: 1px solid #22c55e; border-radius: 8px; padding: 14px; margin-bottom: 10px; }\n    .alert-banner .alert-title { color: #22c55e; font-weight: 700; font-size: 1rem; }\n    .alert-banner .alert-body  { color: #86efac; font-size: 0.85rem; margin-top: 4px; }\n    .empty { color: #475569; font-size: 0.85rem; text-align: center; padding: 30px 0; }\n    .full-width { grid-column: 1 / -1; }\n  </style>\n</head>\n<body>\n\n<h1>🛒 Comprai — Price Watcher</h1>\n<p class=\"subtitle\">Teste em tempo real via SignalR · Backend: <span id=\"apiUrl\"></span></p>\n\n<div class=\"grid\">\n\n  <!-- Config -->\n  <div class=\"card\">\n    <h2>⚙️ Configuração</h2>\n    <label>API Base URL</label>\n    <input id=\"baseUrl\" value=\"\" />\n    <label>Session ID</label>\n    <input id=\"sessionId\" value=\"session-teste-001\" />\n    <button class=\"btn-primary\" onclick=\"connectSignalR()\">🔌 Conectar SignalR</button>\n    <div class=\"status-bar\" style=\"margin-top:12px\">\n      <div class=\"dot\" id=\"dot\"></div>\n      <span id=\"connStatus\">Desconectado</span>\n    </div>\n  </div>\n\n  <!-- Adicionar Watch -->\n  <div class=\"card\">\n    <h2>➕ Adicionar Watch</h2>\n    <label>Product ID</label>\n    <input id=\"productId\" value=\"SKU12345-BR\" />\n    <label>Título</label>\n    <input id=\"title\" value=\"Notebook Samsung BR\" />\n    <label>Preço Atual (R$)</label>\n    <input id=\"currentPrice\" type=\"number\" value=\"2999.90\" step=\"0.01\" />\n    <label>Preço Alvo (R$)</label>\n    <input id=\"targetPrice\" type=\"number\" value=\"2500.00\" step=\"0.01\" />\n    <label>Email (opcional)</label>\n    <input id=\"email\" type=\"email\" placeholder=\"seu@email.com\" />\n    <button class=\"btn-primary\" onclick=\"addWatch()\">📌 Monitorar Preço</button>\n  </div>\n\n  <!-- Alertas -->\n  <div class=\"card\">\n    <h2>🔔 Alertas Recebidos</h2>\n    <div id=\"alertList\"><p class=\"empty\">Nenhum alerta ainda — aguardando SignalR...</p></div>\n  </div>\n\n  <!-- Watches ativos -->\n  <div class=\"card\">\n    <h2>👁️ Watches Ativos</h2>\n    <button class=\"btn-info\" onclick=\"listWatches()\" style=\"margin-bottom:12px\">🔄 Atualizar</button>\n    <div class=\"watch-list\" id=\"watchList\"><p class=\"empty\">Clique em Atualizar</p></div>\n  </div>\n\n  <!-- Log -->\n  <div class=\"card full-width\">\n    <h2>📋 Log</h2>\n    <div class=\"log\" id=\"log\"></div>\n  </div>\n\n</div>\n\n<script>\n  let connection = null;\n\n  function log(msg, type = 'info') {\n    const el = document.getElementById('log');\n    const d  = new Date().toLocaleTimeString('pt-BR');\n    el.innerHTML += `<div class=\"log-entry ${type}\">[${d}] ${msg}</div>`;\n    el.scrollTop  = el.scrollHeight;\n  }\n\n  function getBase() { const v = document.getElementById('baseUrl').value.trim(); return v || window.location.origin; }\n  function getSession() { return document.getElementById('sessionId').value.trim(); }\n\n  document.getElementById('apiUrl').textContent = getBase();\n\n  async function connectSignalR() {\n    if (connection) { await connection.stop(); }\n    const url = getBase() + '/hubs/price';\n    log(`Conectando em ${url}...`, 'info');\n\n    connection = new signalR.HubConnectionBuilder()\n      .withUrl(url)\n      .withAutomaticReconnect()\n      .build();\n\n    connection.on('PriceAlert', (alert) => {\n      log(`🎉 ALERTA: ${alert.title} — de R$${alert.oldPrice} para R$${alert.newPrice} (alvo: R$${alert.targetPrice})`, 'alert');\n      const list = document.getElementById('alertList');\n      if (list.querySelector('.empty')) list.innerHTML = '';\n      list.innerHTML = `\n        <div class=\"alert-banner\">\n          <div class=\"alert-title\">🎉 ${alert.title}</div>\n          <div class=\"alert-body\">\n            Preço baixou de <b>R$ ${alert.oldPrice.toFixed(2)}</b> para\n            <b style=\"color:#22c55e\">R$ ${alert.newPrice.toFixed(2)}</b>\n            (seu alvo era R$ ${alert.targetPrice.toFixed(2)})\n            <br><small style=\"color:#4ade80\">Watch ID: ${alert.watchId} · ${new Date(alert.alertedAt).toLocaleString('pt-BR')}</small>\n          </div>\n        </div>` + list.innerHTML;\n    });\n\n    connection.onreconnecting(() => {\n      log('Reconectando...', 'warn');\n      document.getElementById('dot').classList.remove('connected');\n      document.getElementById('connStatus').textContent = 'Reconectando...';\n    });\n\n    connection.onreconnected(() => {\n      log('Reconectado!', 'info');\n      joinGroup();\n    });\n\n    try {\n      await connection.start();\n      log('SignalR conectado ✅', 'info');\n      document.getElementById('dot').classList.add('connected');\n      document.getElementById('connStatus').textContent = 'Conectado';\n      await joinGroup();\n    } catch(e) {\n      log('Erro ao conectar: ' + e.message, 'error');\n    }\n  }\n\n  async function joinGroup() {\n    const session = getSession();\n    await connection.invoke('JoinSession', session);\n    log(`Entrou no grupo: ${session}`, 'info');\n  }\n\n  async function addWatch() {\n    const base    = getBase();\n    const session = getSession();\n    const body = {\n      productId:    document.getElementById('productId').value,\n      title:        document.getElementById('title').value,\n      currentPrice: parseFloat(document.getElementById('currentPrice').value),\n      targetPrice:  parseFloat(document.getElementById('targetPrice').value),\n      imageUrl:     null,\n      email:        document.getElementById('email').value || null\n    };\n\n    log(`Adicionando watch: ${body.title} alvo=R$${body.targetPrice}`, 'info');\n\n    try {\n      const res  = await fetch(`${base}/api/price-watch?sessionId=${session}`, {\n        method: 'POST',\n        headers: { 'Content-Type': 'application/json' },\n        body: JSON.stringify(body)\n      });\n      const data = await res.json();\n      log(`Watch criado: ${data.watchId} — ${data.message}`, 'info');\n      await listWatches();\n    } catch(e) {\n      log('Erro ao adicionar watch: ' + e.message, 'error');\n    }\n  }\n\n  async function listWatches() {\n    const base    = getBase();\n    const session = getSession();\n    try {\n      const res  = await fetch(`${base}/api/price-watch/${session}`);\n      const data = await res.json();\n      const list = document.getElementById('watchList');\n      if (!data.watches.length) {\n        list.innerHTML = '<p class=\"empty\">Nenhum watch ativo</p>';\n        return;\n      }\n      list.innerHTML = data.watches.map(w => `\n        <div class=\"watch-item\">\n          <span class=\"title\">${w.title}</span>\n          <button class=\"remove\" onclick=\"removeWatch('${w.watchId}')\">✕</button>\n          <div class=\"prices\">\n            <span>Atual: <b>R$ ${w.currentPrice.toFixed(2)}</b></span>\n            <span>Alvo: <b>R$ ${w.targetPrice.toFixed(2)}</b></span>\n            <span>ID: <b>${w.watchId}</b></span>\n          </div>\n        </div>`).join('');\n      log(`${data.total} watch(es) ativo(s)`, 'info');\n    } catch(e) {\n      log('Erro ao listar: ' + e.message, 'error');\n    }\n  }\n\n  async function triggerAlert(watchId) {\n    const base    = getBase();\n    const session = getSession();\n    log(`⚡ Disparando alerta manual: ${watchId}`, 'warn');\n    try {\n      const res  = await fetch(`${base}/api/price-watch/trigger-test?sessionId=${session}&watchId=${watchId}`, { method: 'POST' });\n      const data = await res.json();\n      log(`✅ ${data.message}`, 'alert');\n      await listWatches();\n    } catch(e) {\n      log('Erro ao disparar: ' + e.message, 'error');\n    }\n  }\n\n  async function removeWatch(watchId) {\n    const base    = getBase();\n    const session = getSession();\n    try {\n      await fetch(`${base}/api/price-watch/${session}/${watchId}`, { method: 'DELETE' });\n      log(`Watch ${watchId} removido`, 'warn');\n      await listWatches();\n    } catch(e) {\n      log('Erro ao remover: ' + e.message, 'error');\n    }\n  }\n</script>\n</body>\n</html>\n";
app.MapGet("/price-watcher-test", () => Results.Content(pwHtml, "text/html"))
   .WithTags("PriceWatcher")
   .ExcludeFromDescription();

app.Run();

// â”€â”€ Request DTOs â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
record AddToCartRequest(UcpAgent.SharedKernel.Models.ProductDto Product, int Quantity = 1);








public partial class Program { }
