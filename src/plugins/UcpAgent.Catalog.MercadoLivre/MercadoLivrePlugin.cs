using UcpAgent.SharedKernel.Attributes;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Catalog.MercadoLivre;

[CatalogPlugin("MercadoLivre")]
public sealed class MercadoLivrePlugin : IProductCatalogPort
{
    public string SourceName => "MercadoLivre";

    public Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(new SearchResult([], 0, request.Page, request.PageSize, SourceName));
}
