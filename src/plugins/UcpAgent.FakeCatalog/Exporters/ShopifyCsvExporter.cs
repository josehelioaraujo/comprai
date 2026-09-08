using System.Globalization;
using System.Text;
using UcpAgent.SharedKernel.Models;

namespace UcpAgent.FakeCatalog.Exporters;

public sealed class ShopifyCsvExporter : ICatalogExporter
{
    public string Format => "shopify-csv";

    public string Export(IReadOnlyList<ProductDto> products)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Title,URL handle,Description,Vendor,Type,Tags,Published on online store,Status,SKU,Option1 name,Option1 value,Price,Compare-at price,Inventory quantity,Weight value (grams),Requires shipping,Product image URL");

        foreach (var p in products)
        {
            var vendor      = p.Title.Split(' ').Last();
            var vendorLower = vendor.ToLower();
            var handle      = p.Id.Replace("fake-", "");
            var sku         = p.Id.Replace("fake-", "").ToUpper();
            var tags        = $"{p.Category}, Brasil, {vendor}, {vendorLower}";
            var price       = p.Price.ToString("F2", CultureInfo.InvariantCulture);
            var compare     = p.OriginalPrice?.ToString("F2", CultureInfo.InvariantCulture) ?? "";
            var qty         = p.AvailableQuantity?.ToString() ?? "100";
            var desc        = $"Produto {p.Title} — melhor preço para o mercado brasileiro.";

            sb.AppendLine(string.Join(",", [
                Csv(p.Title), Csv(handle), Csv(desc), Csv(vendor),
                Csv(p.Category ?? ""), Csv(tags),
                "TRUE", "Active", Csv(sku),
                "Título", "Padrão",
                price, compare, qty, "300", "TRUE",
                Csv(p.ImageUrl ?? "")
            ]));
        }
        return sb.ToString();
    }

    private static string Csv(string v) =>
        v.Contains(',') || v.Contains('"') || v.Contains('\n')
            ? $"\"{v.Replace("\"", "\"\"")}\"" : v;
}
