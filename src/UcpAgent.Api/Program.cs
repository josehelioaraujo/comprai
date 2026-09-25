using UcpAgent.SharedKernel;
using System.Text.Json;
using UcpAgent.Api.Health;
using UcpAgent.Api.Resilience;
using UcpAgent.Api.RateLimit;
using UcpAgent.Api.Adapters;
using UcpAgent.Application.Payment;
using UcpAgent.Catalog.Shopify;
using UcpAgent.Application.Orders;
using UcpAgent.Api.Endpoints;
using UcpAgent.Api;
using Scalar.AspNetCore;
using MediatR;
using UcpAgent.Application.IntentRouter;
using UcpAgent.Infrastructure.Messaging;
using Microsoft.Extensions.Caching.Hybrid;
using OpenTelemetry.Metrics;
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
using UcpAgent.Catalog.DummyJSON;
using UcpAgent.PriceWatcher;
using UcpAgent.PriceWatcher.Channels;
using UcpAgent.PriceWatcher.Hubs;
using UcpAgent.Api.Cache;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddObservabilidade(builder.Configuration);
builder.Services.AddSingleton<UcpMetrics>();

// Ã¢ââ¬Ã¢ââ¬ MediatR Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<SearchProductsHandler>());

// Ã¢ââ¬Ã¢ââ¬ HybridCache + Redis Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬
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

// ── Cache Strategy ──────────────────────────────────────────────────────────
var cacheStrategyStr = builder.Configuration["Features:CacheStrategy"] ?? "hybrid";
var cacheStrategy = cacheStrategyStr.ToLowerInvariant() switch
{
    "aside"        => UcpAgent.SharedKernel.CacheStrategy.Aside,
    "read-through" => UcpAgent.SharedKernel.CacheStrategy.ReadThrough,
    "ttl"          => UcpAgent.SharedKernel.CacheStrategy.Ttl,
    _              => UcpAgent.SharedKernel.CacheStrategy.Hybrid
};
builder.Services.AddSingleton<UcpAgent.SharedKernel.Ports.ICacheService>(
    sp => new UcpCacheService(sp.GetRequiredService<HybridCache>(), cacheStrategy));

// Ã¢ââ¬Ã¢ââ¬ OpenTelemetry Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("comprai-api"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(UcpMetrics.MeterName)
        .AddPrometheusExporter());

// Ã¢ââ¬Ã¢ââ¬ OpenAPI Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬
builder.Services.AddOpenApi();

// Ã¢ââ¬Ã¢ââ¬ Health Checks Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬
builder.Services.AddHealthChecks();

// Ã¢ââ¬Ã¢ââ¬ Ports / Adapters Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬
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
    // Plugins de catÃÂ¡logo
    builder.Services.AddHttpClient<MercadoLivrePlugin>()
        .AddCatalogResilience(builder.Configuration);
    builder.Services.AddSingleton<IProductCatalogPort, MercadoLivrePlugin>();
    builder.Services.AddHttpClient<VtexCatalogPlugin>()
        .AddCatalogResilience(builder.Configuration);
    builder.Services.AddSingleton<IProductCatalogPort, VtexCatalogPlugin>();
    builder.Services.AddHttpClient<VtexSearchPlugin>()
        .AddCatalogResilience(builder.Configuration);
    builder.Services.AddSingleton<IProductCatalogPort, VtexSearchPlugin>();
    builder.Services.AddHttpClient<OpenFoodFactsPlugin>()
        .AddCatalogResilience(builder.Configuration);
    builder.Services.Configure<ShopifyOptions>(builder.Configuration.GetSection("Shopify"));
    builder.Services.AddHttpClient<ShopifyPlugin>(c => { c.Timeout = TimeSpan.FromSeconds(15); })
        .AddCatalogResilience(builder.Configuration);
    builder.Services.AddTransient<IProductCatalogPort, ShopifyPlugin>();
    builder.Services.AddSingleton<IProductCatalogPort, OpenFoodFactsPlugin>();

    if (usarRedis)
    {
        // ConexÃÂ£o Redis compartilhada
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

// Ã¢ââ¬Ã¢ââ¬ IEventPublisher Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬
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


// ââ Payment ââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ
var paymentProvider = builder.Configuration["Features:PaymentProvider"] ?? "mock";
if (paymentProvider == "stripe")
    builder.Services.AddSingleton<IPaymentPort, StripePaymentAdapter>();
else if (paymentProvider == "efipay")
    builder.Services.AddEfiPayment(builder.Configuration);
else
    builder.Services.AddSingleton<IPaymentPort, MockPaymentAdapter>();

builder.Services.AddSingleton<IIntentRouterService, IntentRouterService>();


// ââ Price Watcher ââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ
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

// ââ DummyJSON Plugin ââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ
builder.Services.AddHttpClient<DummyJsonPlugin>()
    .AddCatalogResilience(builder.Configuration);
builder.Services.AddSingleton<IProductCatalogPort, DummyJsonPlugin>();

builder.Services.AddCatalogRateLimiter(builder.Configuration);

builder.Services.AddStatusPageHealthChecks(builder.Configuration);


// Observability HttpClient (interno — sem auth)
builder.Services.AddHttpClient("observability", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// GitHub HttpClient
builder.Services.AddHttpClient("github", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var pat    = config["GitHub:Pat"] ?? Environment.GetEnvironmentVariable("GH_PAT") ?? string.Empty;
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {pat}");
    client.DefaultRequestHeaders.Add("User-Agent",    "comprai-qa-hub/1.0");
    client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapOpenApi();
app.MapScalarApiReference();

// Ã¢ââ¬Ã¢ââ¬ Health Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }))
   .WithTags("Health");

app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }))
   .WithTags("Health");

// Ã¢ââ¬Ã¢ââ¬ Search (cache 5 min) Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬
app.MapGet("/api/search", async (
    string q, int page, int pageSize,
    string? category, decimal? minPrice, decimal? maxPrice,
    IMediator mediator,
    UcpAgent.SharedKernel.Ports.ICacheService cacheService,
    UcpMetrics metrics,
    CancellationToken ct) =>
{
    var cacheKey = $"search:{q.ToLowerInvariant().Trim()}:{page}:{pageSize}:{category}:{minPrice}:{maxPrice}";
    bool fromCache = true;

    var result = await cacheService.GetOrSetAsync(
        cacheKey,
        async token =>
        {
            fromCache = false;
            metrics.CacheMissTotal.Add(1);
            var r = await mediator.Send(
                new SearchProductsQuery(q, page, pageSize, category, minPrice, maxPrice), token);
            // Nao cacheia resultado vazio
            return (r.IsSuccess && r.Value.Items.Count > 0) ? r : null;
        },
        ttl: null,
        ct: ct);

    if (fromCache && result != null) metrics.CacheHitTotal.Add(1);

    // Se factory retornou null (sem resultados), executa sem cachear
    var final = result ?? await mediator.Send(
        new SearchProductsQuery(q, page, pageSize, category, minPrice, maxPrice), ct);

    return final.IsSuccess ? Results.Ok(final.Value) : Results.Problem(final.Error);
})
.WithTags("Search").WithName("SearchProducts")
   .RequireRateLimiting(RateLimitExtensions.CatalogPolicy);

// Ã¢ââ¬Ã¢ââ¬ Cart Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬
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

// Ã¢ââ¬Ã¢ââ¬ Checkout Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬
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

// Ã¢ââ¬Ã¢ââ¬ Order Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬Ã¢ââ¬
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

// ââ Payment ââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ
app.MapPost("/api/payment/{orderId}", async (
    string orderId, PaymentRequestDto req,
    IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(
        new ProcessPaymentCommand(orderId, req.Amount, req.Currency, req.Method), ct);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { error = result.Error });
})
.WithTags("Payment").WithName("ProcessPayment");

app.MapIntentEndpoints();
app.MapObservabilityEndpoints();
app.MapContainerEndpoints();
app.MapFakeCatalogEndpoints();

var pwEnabled = app.Configuration.GetValue<bool>("Features:UsarPriceWatcher");
if (pwEnabled)
{
    app.MapHub<PriceHub>("/hubs/price");
    app.MapPriceWatcherEndpoints();
}
app.MapGet("/api/ml/token-debug", async (UcpAgent.Catalog.MercadoLivreOrders.MlTokenService ml, CancellationToken ct) => { try { var t = await ml.GetAccessTokenAsync(ct); return Results.Ok(new { access_token = t }); } catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); } }).WithTags("Debug");



// ââ Price Watcher Test Page ââââââââââââââââââââââââââââââââââââââââââââââââââââ
app.MapGet("/price-watcher-test", async (CancellationToken ct) =>
{
    // Procura em docs/ relativo ao repositÃ³rio na VPS, depois em wwwroot/ como fallback
    var candidates = new[]
    {
        Path.Combine(AppContext.BaseDirectory, "wwwroot", "price-watcher-test.html"),
        Path.Combine(Directory.GetCurrentDirectory(), "docs", "price-watcher-test.html"),
    };
    var path = candidates.FirstOrDefault(File.Exists);
    if (path is null) return Results.NotFound("price-watcher-test.html nÃ£o encontrado");
    var html = await File.ReadAllTextAsync(path, ct);
    return Results.Content(html, "text/html");
})
.WithTags("PriceWatcher")
.ExcludeFromDescription();

// ── K6 Analyze ────────────────────────────────────────────────────────────────
app.MapGet("/api/k6/models", async (IHttpClientFactory factory, CancellationToken ct) =>
{
    try
    {
        var ollamaUrl = app.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        var client = factory.CreateClient();
        var resp = await client.GetAsync($"{ollamaUrl}/api/tags", ct);
        if (!resp.IsSuccessStatusCode)
            return Results.Problem("Ollama não respondeu", statusCode: 502);
        var json = await resp.Content.ReadAsStringAsync(ct);
        // Filtrar modelos de embedding (não geram texto)
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var filtered = doc.RootElement.GetProperty("models")
            .EnumerateArray()
            .Where(m =>
            {
                var name = m.GetProperty("name").GetString() ?? "";
                var family = m.TryGetProperty("details", out var det) &&
                             det.TryGetProperty("family", out var fam)
                             ? fam.GetString() ?? "" : "";
                // Excluir modelos de embedding
                return !name.Contains("embed") && !name.Contains("nomic") &&
                       !family.Contains("bert") && !family.Contains("nomic");
            })
            .Select(m => m)
            .ToList();
        var result = System.Text.Json.JsonSerializer.Serialize(new { models = filtered });
        return Results.Content(result, "application/json");
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 503);
    }
})
.WithTags("K6")
.WithName("GetOllamaModels");

app.MapPost("/api/k6/analyze", async (K6AnalyzeRequest req, IHttpClientFactory factory, UcpMetrics metrics, CancellationToken ct) =>
{
    try
    {
        var ollamaUrl = app.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        var client = factory.CreateClient();

        var systemPrompt = """
            Você é um especialista em performance de APIs e testes de carga com k6.
            Analise os dados do summary.json do k6 e responda de forma clara e objetiva em português brasileiro.
            Seja direto, use números concretos dos dados fornecidos e dê recomendações práticas quando relevante.
            Formate a resposta com seções curtas usando markdown simples.
            """;

        var userPrompt = $"""
            Dados do run de teste k6:
            ```json
            {req.Summary}
            ```

            Pergunta: {req.Question}
            """;

        var oSw = System.Diagnostics.Stopwatch.StartNew();
    var ollamaModel = req.Model ?? "gemma3:latest";
    metrics.OllamaRequestTotal.Add(1, new KeyValuePair<string, object?>("model", ollamaModel));
    var ollamaBody = new
        {
            model = ollamaModel,
            prompt = $"Sistema: {systemPrompt}\n\nUsuário: {userPrompt}",
            stream = false
        };

        var payload = System.Text.Json.JsonSerializer.Serialize(ollamaBody);
        var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        var resp = await client.PostAsync($"{ollamaUrl}/api/generate", content, ct);

        oSw.Stop();
        metrics.OllamaDurationMs.Record(oSw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("model", ollamaModel));
        if (!resp.IsSuccessStatusCode)
        {
            metrics.OllamaErrorTotal.Add(1, new KeyValuePair<string, object?>("model", ollamaModel));
            return Results.Problem("Ollama retornou erro", statusCode: 502);
        }

        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var answer = doc.RootElement.GetProperty("response").GetString() ?? "";

        return Results.Ok(new { answer, model = req.Model });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 503);
    }
})
.WithTags("K6")
.WithName("AnalyzeK6Run");


// ── K6 Runs — armazena e serve resultados dos stress tests ────────────────────
var k6Runs = new System.Collections.Concurrent.ConcurrentDictionary<string, object>();

app.MapPost("/api/k6/runs", async (HttpContext ctx, CancellationToken ct) =>
{
    try
    {
        using var reader = new System.IO.StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        using var doc = System.Text.Json.JsonDocument.Parse(body);
        var runId = doc.RootElement.TryGetProperty("run", out var r) ? r.GetString() ?? "0" : "0";
        k6Runs[runId] = System.Text.Json.JsonSerializer.Deserialize<object>(body)!;
        k6Runs["latest"] = k6Runs[runId];
        return Results.Ok(new { saved = true, runId });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 400);
    }
})
.WithTags("K6").WithName("SaveK6Run");

app.MapGet("/api/k6/runs", () =>
{
    var runs = k6Runs.Keys.Where(k => k != "latest").OrderByDescending(k => k).Take(20);
    return Results.Ok(new { runs });
})
.WithTags("K6").WithName("ListK6Runs");

app.MapGet("/api/k6/runs/{runId}", (string runId) =>
{
    if (k6Runs.TryGetValue(runId, out var run))
        return Results.Ok(run);
    return Results.NotFound(new { error = $"Run {runId} não encontrado" });
})
.WithTags("K6").WithName("GetK6Run");

// Servir dashboard K6 em /k6
app.MapGet("/k6", async (HttpContext ctx, CancellationToken ct) =>
{
    // /app/wwwroot/k6/index.html — copiado pelo Dockerfile
    var path = Path.Combine(AppContext.BaseDirectory, "wwwroot", "k6", "index.html");
    if (!File.Exists(path))
        path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "k6", "index.html");
    if (File.Exists(path))
    {
        ctx.Response.ContentType = "text/html; charset=utf-8";
        await ctx.Response.SendFileAsync(path, ct);
        return;
    }
    ctx.Response.StatusCode = 404;
    await ctx.Response.WriteAsync("Dashboard K6 não encontrado em wwwroot/k6/index.html", ct);
})
.WithTags("K6").WithName("K6Dashboard");

app.MapHealthStatusEndpoint();


// ── GitHub Dispatch ────────────────────────────────────────────────────────────
app.MapPost("/api/github/dispatch", async (GitHubDispatchRequest req, IConfiguration config, IHttpClientFactory factory) =>
{
    var ghPat = config["GitHub:Pat"]
             ?? Environment.GetEnvironmentVariable("GH_PAT")
             ?? string.Empty;

    if (string.IsNullOrWhiteSpace(ghPat))
        return Results.Problem("GH_PAT não configurado no servidor.", statusCode: 503);

    var repo     = req.Repo     ?? "josehelioaraujo/comprai";
    var workflow = req.Workflow ?? "integration-tests.yml";
    var branch   = req.Ref      ?? "main";

    // admin-restart so via /api/admin/restart (senha validada no servidor)
    if (workflow.Equals("admin-restart.yml", StringComparison.OrdinalIgnoreCase))
        return Results.Problem("Use /api/admin/restart.", statusCode: 403);

    var url = $"https://api.github.com/repos/{repo}/actions/workflows/{workflow}/dispatches";

    var client = factory.CreateClient("github");
    // Incluir inputs se fornecidos (ex: test_type para stress-tests)
    object dispatchPayload = req.Inputs != null && req.Inputs.Count > 0
        ? new { @ref = branch, inputs = req.Inputs }
        : new { @ref = branch };
    var body   = JsonSerializer.Serialize(dispatchPayload);
    var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

    var resp = await client.PostAsync(url, content);

    if (!resp.IsSuccessStatusCode)
    {
        var err = await resp.Content.ReadAsStringAsync();
        return Results.Problem($"GitHub API: {(int)resp.StatusCode} — {err}", statusCode: 502);
    }

    // Aguardar 5s e retornar o run mais recente
    await Task.Delay(5000);
    var runsUrl = $"https://api.github.com/repos/{repo}/actions/workflows/{workflow}/runs?per_page=1&branch={branch}";
    var runsResp = await client.GetAsync(runsUrl);
    if (runsResp.IsSuccessStatusCode)
    {
        var runsJson = await runsResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(runsJson);
        var run = doc.RootElement.GetProperty("workflow_runs").EnumerateArray().FirstOrDefault();
        if (run.ValueKind != JsonValueKind.Undefined)
        {
            return Results.Ok(new
            {
                runId  = run.GetProperty("id").GetInt64(),
                url    = run.GetProperty("html_url").GetString(),
                status = run.GetProperty("status").GetString()
            });
        }
    }

    return Results.Ok(new { runId = (long?)null, url = (string?)null, status = "queued" });
})
.WithName("GitHubDispatch")
.WithTags("GitHub")
.AllowAnonymous();

// ── Admin Restart (senha validada no servidor, sem expor ao GitHub) ─────────────
app.MapPost("/api/admin/restart", async (AdminRestartRequest req, IConfiguration config, IHttpClientFactory factory) =>
{
    var expected = config["Admin:RestartPassword"]
                ?? Environment.GetEnvironmentVariable("ADMIN_RESTART_PASSWORD")
                ?? string.Empty;
    if (string.IsNullOrWhiteSpace(expected))
        return Results.Problem("ADMIN_RESTART_PASSWORD não configurado no servidor.", statusCode: 503);

    var allowed = new[] { "api", "redis", "all", "redeploy" };
    if (string.IsNullOrWhiteSpace(req.Target) || !allowed.Contains(req.Target))
        return Results.Problem("Target inválido.", statusCode: 400);

    var a = System.Text.Encoding.UTF8.GetBytes(req.Password ?? string.Empty);
    var b = System.Text.Encoding.UTF8.GetBytes(expected);
    if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(a, b))
    {
        await Task.Delay(1000); // freia tentativa por forca bruta
        return Results.Problem("Senha incorreta.", statusCode: 401);
    }

    var ghPat = config["GitHub:Pat"] ?? Environment.GetEnvironmentVariable("GH_PAT") ?? string.Empty;
    if (string.IsNullOrWhiteSpace(ghPat))
        return Results.Problem("GH_PAT não configurado no servidor.", statusCode: 503);

    const string repo = "josehelioaraujo/comprai";
    const string workflow = "admin-restart.yml";
    var client = factory.CreateClient("github");
    var body = JsonSerializer.Serialize(new { @ref = "main", inputs = new Dictionary<string, string> { ["target"] = req.Target } });
    var resp = await client.PostAsync($"https://api.github.com/repos/{repo}/actions/workflows/{workflow}/dispatches",
        new StringContent(body, System.Text.Encoding.UTF8, "application/json"));
    if (!resp.IsSuccessStatusCode)
        return Results.Problem($"GitHub API: {(int)resp.StatusCode}", statusCode: 502);

    await Task.Delay(4000);
    var runsResp = await client.GetAsync($"https://api.github.com/repos/{repo}/actions/workflows/{workflow}/runs?per_page=1&branch=main");
    if (runsResp.IsSuccessStatusCode)
    {
        using var doc = JsonDocument.Parse(await runsResp.Content.ReadAsStringAsync());
        var run = doc.RootElement.GetProperty("workflow_runs").EnumerateArray().FirstOrDefault();
        if (run.ValueKind != JsonValueKind.Undefined)
            return Results.Ok(new { runId = run.GetProperty("id").GetInt64(), url = run.GetProperty("html_url").GetString(), status = run.GetProperty("status").GetString() });
    }
    return Results.Ok(new { runId = (long?)null, url = (string?)null, status = "queued" });
})
.WithName("AdminRestart")
.WithTags("Admin")
.AllowAnonymous();

// ── GitHub Run Status ──────────────────────────────────────────────────────────
app.MapGet("/api/github/run/{runId}/status", async (long runId, string? repo, IConfiguration config, IHttpClientFactory factory) =>
{
    var ghPat = config["GitHub:Pat"]
             ?? Environment.GetEnvironmentVariable("GH_PAT")
             ?? string.Empty;

    if (string.IsNullOrWhiteSpace(ghPat))
        return Results.Problem("GH_PAT não configurado.", statusCode: 503);

    repo ??= "josehelioaraujo/comprai";
    var client = factory.CreateClient("github");

    var runResp  = await client.GetAsync($"https://api.github.com/repos/{repo}/actions/runs/{runId}");
    var jobsResp = await client.GetAsync($"https://api.github.com/repos/{repo}/actions/runs/{runId}/jobs");

    if (!runResp.IsSuccessStatusCode)
        return Results.Problem("Erro ao consultar run.", statusCode: 502);

    var runJson  = await runResp.Content.ReadAsStringAsync();
    var jobsJson = jobsResp.IsSuccessStatusCode ? await jobsResp.Content.ReadAsStringAsync() : "{}";

    using var runDoc  = JsonDocument.Parse(runJson);
    using var jobsDoc = JsonDocument.Parse(jobsJson);

    var run  = runDoc.RootElement;
    JsonElement jobsEl;
    jobsDoc.RootElement.TryGetProperty("jobs", out jobsEl);

    return Results.Ok(new
    {
        id         = run.GetProperty("id").GetInt64(),
        status     = run.GetProperty("status").GetString(),
        conclusion = run.TryGetProperty("conclusion", out var c) ? c.GetString() : null,
        htmlUrl    = run.GetProperty("html_url").GetString(),
        createdAt  = run.GetProperty("created_at").GetString(),
        updatedAt  = run.GetProperty("updated_at").GetString(),
        jobs       = jobsEl.ValueKind == JsonValueKind.Array
            ? jobsEl.EnumerateArray().Select(j => new
            {
                name       = j.GetProperty("name").GetString(),
                status     = j.GetProperty("status").GetString(),
                conclusion = j.TryGetProperty("conclusion", out var jc) ? jc.GetString() : null,
                startedAt  = j.TryGetProperty("started_at", out var js) ? js.GetString() : null,
                completedAt= j.TryGetProperty("completed_at", out var jcp) ? jcp.GetString() : null,
                steps      = j.TryGetProperty("steps", out var st) && st.ValueKind == JsonValueKind.Array
                    ? st.EnumerateArray().Select(s => new
                    {
                        name        = s.GetProperty("name").GetString(),
                        status      = s.GetProperty("status").GetString(),
                        conclusion  = s.TryGetProperty("conclusion", out var sc) ? sc.GetString() : null,
                        startedAt   = s.TryGetProperty("started_at", out var ss) ? ss.GetString() : null,
                        completedAt = s.TryGetProperty("completed_at", out var scp) ? scp.GetString() : null,
                    }).ToList()
                    : null
            }).ToList()
            : null
    });
})
.WithName("GitHubRunStatus")
.WithTags("GitHub")
.AllowAnonymous();

// ── GitHub Run Cancel ─────────────────────────────────────────────────────────
app.MapPost("/api/github/run/{runId}/cancel", async (long runId, string? repo, IConfiguration config, IHttpClientFactory factory) =>
{
    var ghPat = config["GitHub:Pat"]
             ?? Environment.GetEnvironmentVariable("GH_PAT")
             ?? string.Empty;
    if (string.IsNullOrEmpty(ghPat))
        return Results.Problem("GH_PAT não configurado", statusCode: 500);

    repo ??= config["GitHub:Repo"] ?? "josehelioaraujo/comprai";
    var client = factory.CreateClient("github");

    var resp = await client.PostAsync(
        $"https://api.github.com/repos/{repo}/actions/runs/{runId}/cancel",
        new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

    // 202 Accepted = cancelamento aceito, 409 = já concluído
    if (resp.StatusCode == System.Net.HttpStatusCode.Accepted)
        return Results.Accepted();
    if (resp.StatusCode == System.Net.HttpStatusCode.Conflict)
        return Results.Conflict(new { message = "Run já concluído ou não pode ser cancelado" });

    var err = await resp.Content.ReadAsStringAsync();
    return Results.Problem($"GitHub API: {(int)resp.StatusCode} — {err}", statusCode: 502);
})
.WithName("GitHubRunCancel")
.WithTags("GitHub");

// ── GitHub Run Latest ─────────────────────────────────────────────────────────
app.MapGet("/api/github/run/latest", async (string? workflow, string? repo, IConfiguration config, IHttpClientFactory factory) =>
{
    var ghPat = config["GitHub:Pat"]
             ?? Environment.GetEnvironmentVariable("GH_PAT")
             ?? string.Empty;

    if (string.IsNullOrWhiteSpace(ghPat))
        return Results.Problem("GH_PAT não configurado.", statusCode: 503);

    repo     ??= "josehelioaraujo/comprai";
    workflow ??= "stress-tests.yml";

    var client   = factory.CreateClient("github");
    var runsUrl  = $"https://api.github.com/repos/{repo}/actions/workflows/{workflow}/runs?per_page=1";
    var runsResp = await client.GetAsync(runsUrl);

    if (!runsResp.IsSuccessStatusCode)
        return Results.Problem("Erro ao consultar runs.", statusCode: 502);

    var runsJson = await runsResp.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(runsJson);
    var run = doc.RootElement.GetProperty("workflow_runs").EnumerateArray().FirstOrDefault();

    if (run.ValueKind == JsonValueKind.Undefined)
        return Results.Ok(new { runId = (long?)null });

    return Results.Ok(new
    {
        runId      = run.GetProperty("id").GetInt64(),
        runNumber  = run.GetProperty("run_number").GetInt32(),
        status     = run.GetProperty("status").GetString(),
        conclusion = run.TryGetProperty("conclusion", out var c) ? c.GetString() : null,
        htmlUrl    = run.GetProperty("html_url").GetString()
    });
})
.WithName("GitHubRunLatest")
.WithTags("GitHub")
.AllowAnonymous();

app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.Run();

// ââ Request DTOs ââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââââ
record AddToCartRequest(UcpAgent.SharedKernel.Models.ProductDto Product, int Quantity = 1);
record PaymentRequestDto(
    decimal Amount,
    string Currency,
    UcpAgent.SharedKernel.Ports.PaymentMethodDto Method);

record K6AnalyzeRequest(string Summary, string Question, string? Model);

record AdminRestartRequest(string? Target, string? Password);

record GitHubDispatchRequest(string? Repo, string? Workflow, string? Ref, Dictionary<string, string>? Inputs = null);







