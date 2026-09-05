using UcpAgent.SharedKernel.Attributes;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Catalog.VtexSearch;

[CatalogPlugin("VtexSearch")]
public sealed class VtexSearchPlugin : IProductCatalogPort
{
    public string SourceName => "VtexSearch";

    public Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new SearchResult([], 0, request.Page, request.PageSize, SourceName));
}
