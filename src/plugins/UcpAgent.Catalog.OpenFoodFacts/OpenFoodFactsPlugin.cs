using UcpAgent.SharedKernel.Attributes;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Catalog.OpenFoodFacts;

[CatalogPlugin("OpenFoodFacts")]
public sealed class OpenFoodFactsPlugin : IProductCatalogPort
{
    public string SourceName => "OpenFoodFacts";

    public Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new SearchResult([], 0, request.Page, request.PageSize, SourceName));
}
