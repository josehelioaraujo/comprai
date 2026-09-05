using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Search;

public sealed class SearchProductsHandler(IEnumerable<IProductCatalogPort> catalogs)
    : IRequestHandler<SearchProductsQuery, Result<IReadOnlyList<SearchResult>>>
{
    public async Task<Result<IReadOnlyList<SearchResult>>> Handle(
        SearchProductsQuery request, CancellationToken cancellationToken)
    {
        var searchRequest = new SearchRequest(
            request.Query, request.Page, request.PageSize,
            request.Category, request.MinPrice, request.MaxPrice);

        var tasks = catalogs.Select(c => c.SearchAsync(searchRequest, cancellationToken));
        var results = await Task.WhenAll(tasks);

        return Result<IReadOnlyList<SearchResult>>.Ok(results);
    }
}
