using UcpAgent.SharedKernel.Models;

namespace UcpAgent.FakeCatalog.Exporters;

public interface ICatalogExporter
{
    string Format { get; }
    string Export(IReadOnlyList<ProductDto> products);
}
