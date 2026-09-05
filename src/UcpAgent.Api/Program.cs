using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using UcpAgent.Application.Search;
using UcpAgent.Application.Cart;
using UcpAgent.Application.Checkout;
using UcpAgent.Application.Order;
using UcpAgent.Api.Mocks;
using UcpAgent.SharedKernel.Ports;

var builder = WebApplication.CreateBuilder(args);

// ── MediatR ──────────────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<SearchProductsHandler>());

// ── HybridCache + Redis ───────────────────────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(opt =>
    opt.Configuration = builder.Configuration["Redis:ConnectionString"]);

builder.Services.AddHybridCache();

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
var usarMock = builder.Configuration.GetValue<bool>("Features:UsarMockDados");

if (usarMock)
{
    builder.Services.AddSingleton<IProductCatalogPort, MockCatalogPlugin>();
    builder.Services.AddSingleton<ICartPort, InMemoryCartPort>();
    builder.Services.AddSingleton<ICheckoutPort, MockCheckoutPort>();
    builder.Services.AddSingleton<IOrderPort, MockOrderPort>();
}

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

// ── Health ────────────────────────────────────────────────────────────────────
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }))
   .WithTags("Health");

app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }))
   .WithTags("Health");

// ── Search ────────────────────────────────────────────────────────────────────
app.MapGet("/api/search", async (
    string q, int page, int pageSize,
    string? category, decimal? minPrice, decimal? maxPrice,
    IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(
        new SearchProductsQuery(q, page, pageSize, category, minPrice, maxPrice), ct);

    return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error);
})
.WithTags("Search")
.WithName("SearchProducts");

// ── Cart ──────────────────────────────────────────────────────────────────────
app.MapPost("/api/cart/{sessionId}/items", async (
    string sessionId, AddToCartRequest req,
    IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new AddToCartCommand(sessionId, req.Product, req.Quantity), ct);
    return result.IsSuccess ? Results.Ok(new { itemId = result.Value }) : Results.Problem(result.Error);
})
.WithTags("Cart")
.WithName("AddToCart");

// ── Checkout ──────────────────────────────────────────────────────────────────
app.MapPost("/api/checkout/{sessionId}", async (
    string sessionId, CustomerDto customer,
    IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new CheckoutCommand(sessionId, customer), ct);
    return result.IsSuccess ? Results.Ok(result.Value) : Results.Problem(result.Error);
})
.WithTags("Checkout")
.WithName("Checkout");

// ── Order ─────────────────────────────────────────────────────────────────────
app.MapGet("/api/orders/{orderId}", async (
    string orderId, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new GetOrderQuery(orderId), ct);
    return result.IsSuccess
        ? (result.Value is null ? Results.NotFound() : Results.Ok(result.Value))
        : Results.Problem(result.Error);
})
.WithTags("Order")
.WithName("GetOrder");

app.Run();

// ── Request DTOs ──────────────────────────────────────────────────────────────
record AddToCartRequest(UcpAgent.SharedKernel.Models.ProductDto Product, int Quantity = 1);
