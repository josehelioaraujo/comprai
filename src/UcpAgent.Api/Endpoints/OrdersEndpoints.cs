using Microsoft.AspNetCore.Mvc;
using UcpAgent.Infrastructure.Persistence.Repositories;

namespace UcpAgent.Api.Endpoints;

public static class OrdersEndpoints
{
    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/orders").WithTags("Orders");

        // F4 — Meus Pedidos: lê order_history por sessionId
        grp.MapGet("", async (
            [FromQuery] string sessionId,
            [FromServices] OrdersQueryRepository repo,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return Results.BadRequest("sessionId obrigatório");

            var orders = await repo.GetHistoryBySessionAsync(sessionId, ct);
            return Results.Ok(orders);
        })
        .WithName("GetOrdersBySession")
        .WithSummary("Lista pedidos finalizados por sessionId (order_history)");

        // F5 — Timeline ao vivo: lê fulfillment_event por orderId
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
                : Results.Ok(timeline);
        })
        .WithName("GetFulfillmentTimeline")
        .WithSummary("Timeline de fulfillment ao vivo por orderId (fulfillment_event)");

        return app;
    }
}
