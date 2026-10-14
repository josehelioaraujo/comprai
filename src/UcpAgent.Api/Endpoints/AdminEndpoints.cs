using System.Diagnostics.CodeAnalysis;
using UcpAgent.Infrastructure.Persistence.Repositories;

namespace UcpAgent.Api.Endpoints;

[ExcludeFromCodeCoverage]
public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization("Admin");

        // GET /api/admin/stats/summary
        group.MapGet("/stats/summary", async (AdminQueryRepository repo, CancellationToken ct) =>
        {
            var summary = await repo.GetSummaryAsync(ct);
            return Results.Ok(summary);
        })
        .WithName("AdminGetSummary");

        // GET /api/admin/orders?page=1&pageSize=20&status=pending
        group.MapGet("/orders", async (
            AdminQueryRepository repo,
            int page = 1, int pageSize = 20, string? status = null,
            CancellationToken ct = default) =>
        {
            var orders = await repo.GetOrdersAsync(page, pageSize, status, ct);
            return Results.Ok(orders);
        })
        .WithName("AdminGetOrders");

        // GET /api/admin/orders/{id}
        group.MapGet("/orders/{id}", async (string id, AdminQueryRepository repo, CancellationToken ct) =>
        {
            var detail = await repo.GetOrderDetailAsync(id, ct);
            return detail is null ? Results.NotFound() : Results.Ok(detail);
        })
        .WithName("AdminGetOrderDetail");

        // GET /api/admin/stats/payments
        group.MapGet("/stats/payments", async (AdminQueryRepository repo, CancellationToken ct) =>
        {
            var stats = await repo.GetPaymentsStatsAsync(ct);
            return Results.Ok(stats);
        })
        .WithName("AdminGetPaymentsStats");

        // GET /api/admin/stats/fulfillment
        group.MapGet("/stats/fulfillment", async (AdminQueryRepository repo, CancellationToken ct) =>
        {
            var stats = await repo.GetFulfillmentStatsAsync(ct);
            return Results.Ok(stats);
        })
        .WithName("AdminGetFulfillmentStats");

        // GET /api/admin/stats/customers
        group.MapGet("/stats/customers", async (AdminQueryRepository repo, CancellationToken ct) =>
        {
            var stats = await repo.GetCustomersStatsAsync(ct);
            return Results.Ok(stats);
        })
        .WithName("AdminGetCustomersStats");

        // GET /api/admin/charts/sales?days=30
        group.MapGet("/charts/sales", async (AdminQueryRepository repo, int days = 30, CancellationToken ct = default) =>
        {
            var chart = await repo.GetSalesChartAsync(days, ct);
            return Results.Ok(chart);
        })
        .WithName("AdminGetSalesChart");

        // GET /api/admin/charts/top-products?limit=10
        group.MapGet("/charts/top-products", async (AdminQueryRepository repo, int limit = 10, CancellationToken ct = default) =>
        {
            var products = await repo.GetTopProductsAsync(limit, ct);
            return Results.Ok(products);
        })
        .WithName("AdminGetTopProducts");
    }
}
