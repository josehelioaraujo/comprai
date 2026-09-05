using UcpAgent.SharedKernel.Models;

namespace UcpAgent.SharedKernel.Ports;

public interface IProductCatalogPort
{
    string SourceName { get; }
    Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
}
