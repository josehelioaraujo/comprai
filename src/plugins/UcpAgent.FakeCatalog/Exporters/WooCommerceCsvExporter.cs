using System.Globalization;
using System.Text;
using UcpAgent.SharedKernel.Models;

namespace UcpAgent.FakeCatalog.Exporters;

public sealed class WooCommerceCsvExporter : ICatalogExporter
{
    public string Format => "woocommerce-csv";

    public string Export(IReadOnlyList<ProductDto> products)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ID,Type,SKU,Name,Published,Short description,Regular price,Sale price,Categories,Images,Stock");

        var i = 1;
        foreach (var p in products)
        {
            var sku     = p.Id.Replace("fake-", "").ToUpper();
            var regular = p.OriginalPrice?.ToString("F2", CultureInfo.InvariantCulture) ?? p.Price.ToString("F2", CultureInfo.InvariantCulture);
            var sale    = p.Price.ToString("F2", CultureInfo.InvariantCulture);

            sb.AppendLine(string.Join(",", [
                (i++).ToString(), "simple", sku,
                Csv(p.Title), "1",
                Csv($"Compre {p.Title} com o melhor preço."),
                regular, sale,
                Csv(p.Category ?? ""),
                Csv(p.ImageUrl ?? ""),
                (p.AvailableQuantity ?? 100).ToString()
            ]));
        }
        return sb.ToString();
    }

    private static string Csv(string v) =>
        v.Contains(',') || v.Contains('"') || v.Contains('\n')
            ? $"\"{v.Replace("\"", "\"\"")}\"" : v;
}
