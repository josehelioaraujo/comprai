using Microsoft.AspNetCore.Mvc;
using UcpAgent.FakeCatalog;
using UcpAgent.FakeCatalog.Exporters;

namespace UcpAgent.Api.Endpoints;

public static class FakeCatalogEndpoints
{
    public static void MapFakeCatalogEndpoints(this WebApplication app)
    {
        // GET /api/catalog/generate?qty=100&category=eletronicos&format=shopify-csv
        app.MapGet("/api/catalog/generate", (
            [FromQuery] int qty      = 50,
            [FromQuery] string? category = null,
            [FromQuery] string format    = "ucp-json") =>
        {
            qty = Math.Clamp(qty, 1, 1000);
            var products  = FakeProductGenerator.Generate(qty, category);
            var exporters = new List<ICatalogExporter>
            {
                new ShopifyCsvExporter(),
                new WooCommerceCsvExporter(),
                new UcpJsonExporter()
            };

            var exporter = exporters.FirstOrDefault(e =>
                e.Format.Equals(format, StringComparison.OrdinalIgnoreCase));

            if (exporter is null)
                return Results.BadRequest(new
                {
                    error     = $"Formato '{format}' não suportado.",
                    supported = exporters.Select(e => e.Format)
                });

            var content     = exporter.Export(products);
            var contentType = format.EndsWith("csv") ? "text/csv" : "application/json";
            var fileName    = $"comprai-fake-{qty}-{category ?? "all"}.{(format.EndsWith("csv") ? "csv" : "json")}";

            return Results.File(
                System.Text.Encoding.UTF8.GetBytes(content),
                contentType,
                fileName);
        })
        .WithTags("FakeCatalog")
        .WithName("GenerateFakeCatalog")
        .WithSummary("Gera produtos falsos para testes e importação em plataformas de e-commerce");

        // GET /api/catalog/formats
        app.MapGet("/api/catalog/formats", () => Results.Ok(new
        {
            formats = new[]
            {
                new { format = "ucp-json",         description = "JSON no padrão UCP (default)" },
                new { format = "shopify-csv",       description = "CSV compatível com Shopify Admin Import" },
                new { format = "woocommerce-csv",   description = "CSV compatível com WooCommerce Product CSV Import" }
            }
        }))
        .WithTags("FakeCatalog")
        .WithName("ListCatalogFormats");
    }
}
