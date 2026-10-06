using Microsoft.AspNetCore.Mvc;
using UcpAgent.Infrastructure.Persistence.Repositories;

namespace UcpAgent.Api.Endpoints;

public static class OrdersEndpoints
{
    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/orders").WithTags("Orders");

        // GET /api/orders?sessionId — lista pedidos do sessionId via BD
        grp.MapGet("", async (
            [FromQuery] string sessionId,
            [FromServices] OrdersQueryRepository repo,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return Results.BadRequest("sessionId obrigatório");

            var orders = await repo.GetHistoryBySessionAsync(sessionId, ct);

            // Mapeia para o shape que o front espera
            var result = orders.Select(o => new
            {
                orderId        = o.OrderId,
                status         = o.Status,
                total          = o.Total,
                tracking       = o.TrackingCode,
                paymentMethod  = o.PaymentMethod,
                shippingCity   = o.ShippingCity,
                shippingState  = o.ShippingState,
                itemCount      = o.ItemCount,
                createdAt      = o.CreatedAt,
                fulfillmentHistory = Array.Empty<object>(),
            });

            return Results.Ok(result);
        })
        .WithName("GetOrdersBySession")
        .WithSummary("Lista pedidos por sessionId (tabela order + payment)");

        // GET /api/orders/{orderId}/fulfillment — timeline ao vivo
        grp.MapGet("{orderId}/fulfillment", async (
            string orderId,
            [FromServices] OrdersQueryRepository repo,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return Results.BadRequest("orderId obrigatório");

            var timeline = await repo.GetFulfillmentTimelineAsync(orderId, ct);
            return timeline.Count == 0
                ? Results.NotFound()
                : Results.Ok(timeline.Select(e => new
                {
                    status       = e.Status,
                    description  = e.Description,
                    occurredAt   = e.OccurredAt,
                    trackingCode = e.TrackingCode,
                    location     = e.Location,
                }));
        })
        .WithName("GetFulfillmentTimeline")
        .WithSummary("Timeline de fulfillment ao vivo por orderId");

        return app;
    }
}
