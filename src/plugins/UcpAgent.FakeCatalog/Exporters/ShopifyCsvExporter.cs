using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using UcpAgent.SharedKernel.Models;

namespace UcpAgent.FakeCatalog.Exporters;

public sealed class ShopifyCsvExporter : ICatalogExporter
{
    public string Format => "shopify-csv";

    public string Export(IReadOnlyList<ProductDto> products)
    {
        var sb  = new StringBuilder();
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };

        using var writer = new StringWriter(sb);
        using var csv    = new CsvWriter(writer, cfg);

        // Header
        csv.WriteField("Title"); csv.WriteField("URL handle"); csv.WriteField("Description");
        csv.WriteField("Vendor"); csv.WriteField("Type"); csv.WriteField("Tags");
        csv.WriteField("Published on online store"); csv.WriteField("Status");
        csv.WriteField("SKU"); csv.WriteField("Option1 name"); csv.WriteField("Option1 value");
        csv.WriteField("Price"); csv.WriteField("Compare-at price");
        csv.WriteField("Inventory quantity"); csv.WriteField("Weight value (grams)");
        csv.WriteField("Requires shipping"); csv.WriteField("Product image URL");
        csv.NextRecord();

        foreach (var p in products)
        {
            var handle      = p.Id.Replace("fake-sku", "").Trim('-');
            var vendorLower = p.Title.Split(' ').Last().ToLower();
            var tags        = $"{p.Category}, Brasil, {p.Title.Split(' ').Last()}, {vendorLower}";

            csv.WriteField(p.Title);
            csv.WriteField(handle);
            csv.WriteField($"Produto {p.Title} — melhor preço para o mercado brasileiro.");
            csv.WriteField(p.Title.Split(' ').Last()); // vendor = última palavra
            csv.WriteField(p.Category);
            csv.WriteField(tags);
            csv.WriteField("TRUE");
            csv.WriteField("Active");
            csv.WriteField(p.Id.Replace("fake-", "").ToUpper());
            csv.WriteField("Título");
            csv.WriteField("Padrão");
            csv.WriteField(p.Price.ToString("F2", CultureInfo.InvariantCulture));
            csv.WriteField(p.OriginalPrice?.ToString("F2", CultureInfo.InvariantCulture) ?? "");
            csv.WriteField(p.AvailableQuantity?.ToString() ?? "100");
            csv.WriteField("300");
            csv.WriteField("TRUE");
            csv.WriteField(p.ImageUrl ?? "");
            csv.NextRecord();
        }

        return sb.ToString();
    }
}
