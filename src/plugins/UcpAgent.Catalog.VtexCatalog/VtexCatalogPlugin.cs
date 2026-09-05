using UcpAgent.SharedKernel.Attributes;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Catalog.VtexCatalog;

[CatalogPlugin("VtexCatalog")]
public sealed class VtexCatalogPlugin : IProductCatalogPort
{
    public string SourceName => "VtexCatalog";

    public Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new SearchResult([], 0, request.Page, request.PageSize, SourceName));
}
