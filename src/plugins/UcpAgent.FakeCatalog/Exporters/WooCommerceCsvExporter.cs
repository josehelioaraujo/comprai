using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using UcpAgent.SharedKernel.Models;

namespace UcpAgent.FakeCatalog.Exporters;

public sealed class WooCommerceCsvExporter : ICatalogExporter
{
    public string Format => "woocommerce-csv";

    public string Export(IReadOnlyList<ProductDto> products)
    {
        var sb  = new StringBuilder();
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };

        using var writer = new StringWriter(sb);
        using var csv    = new CsvWriter(writer, cfg);

        csv.WriteField("ID"); csv.WriteField("Type"); csv.WriteField("SKU");
        csv.WriteField("Name"); csv.WriteField("Published"); csv.WriteField("Short description");
        csv.WriteField("Regular price"); csv.WriteField("Sale price");
        csv.WriteField("Categories"); csv.WriteField("Images"); csv.WriteField("Stock");
        csv.NextRecord();

        var i = 1;
        foreach (var p in products)
        {
            csv.WriteField(i++);
            csv.WriteField("simple");
            csv.WriteField(p.Id.Replace("fake-", "").ToUpper());
            csv.WriteField(p.Title);
            csv.WriteField("1");
            csv.WriteField($"Compre {p.Title} com o melhor preço.");
            csv.WriteField(p.OriginalPrice?.ToString("F2", CultureInfo.InvariantCulture) ?? p.Price.ToString("F2", CultureInfo.InvariantCulture));
            csv.WriteField(p.Price.ToString("F2", CultureInfo.InvariantCulture));
            csv.WriteField(p.Category);
            csv.WriteField(p.ImageUrl ?? "");
            csv.WriteField(p.AvailableQuantity?.ToString() ?? "100");
            csv.NextRecord();
        }

        return sb.ToString();
    }
}
