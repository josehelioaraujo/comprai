using System.Text.Json;
using UcpAgent.SharedKernel.Models;

namespace UcpAgent.FakeCatalog.Exporters;

public sealed class UcpJsonExporter : ICatalogExporter
{
    public string Format => "ucp-json";

    public string Export(IReadOnlyList<ProductDto> products) =>
        JsonSerializer.Serialize(products, new JsonSerializerOptions { WriteIndented = true });
}
