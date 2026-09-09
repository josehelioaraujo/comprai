using Microsoft.AspNetCore.Mvc;
using UcpAgent.PriceWatcher;
using UcpAgent.PriceWatcher.Models;

namespace UcpAgent.Api.Endpoints;

public static class PriceWatcherEndpoints
{
    public static void MapPriceWatcherEndpoints(this WebApplication app)
    {
        // POST /api/price-watch
        app.MapPost("/api/price-watch", (
            string sessionId,
            [FromBody] PriceWatchRequest req,
            PriceWatcherService svc) =>
        {
            var watchId = svc.AddWatch(sessionId, req);
            return Results.Ok(new { watchId, sessionId, message = $"Monitorando '{req.Title}' — alerta quando <= R$ {req.TargetPrice:F2}" });
        })
        .WithTags("PriceWatcher")
        .WithName("AddPriceWatch")
        .WithSummary("Inicia monitoramento de preço de um produto");

        // GET /api/price-watch/{sessionId}
        app.MapGet("/api/price-watch/{sessionId}", (
            string sessionId,
            PriceWatcherService svc) =>
        {
            var watches = svc.GetWatches(sessionId).ToList();
            return Results.Ok(new { sessionId, total = watches.Count, watches });
        })
        .WithTags("PriceWatcher")
        .WithName("GetPriceWatches");

        // POST /api/price-watch/trigger-test — dispara alerta manualmente (dev/demo)
        app.MapPost("/api/price-watch/trigger-test", async (
            string sessionId, string watchId,
            PriceWatcherService svc,
            CancellationToken ct) =>
        {
            var ok = await svc.TriggerTestAsync(sessionId, watchId, ct);
            return ok ? Results.Ok(new { message = "Alerta disparado via SignalR!" }) : Results.NotFound();
        })
        .WithTags("PriceWatcher")
        .WithName("TriggerTestPriceAlert")
        .WithSummary("Dispara alerta manualmente para demo/teste");

        // DELETE /api/price-watch/{sessionId}/{watchId}
        app.MapDelete("/api/price-watch/{sessionId}/{watchId}", (
            string sessionId, string watchId,
            PriceWatcherService svc) =>
        {
            var removed = svc.RemoveWatch(sessionId, watchId);
            return removed ? Results.NoContent() : Results.NotFound();
        })
        .WithTags("PriceWatcher")
        .WithName("RemovePriceWatch");
    }
}
