using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Models;

namespace UcpAgent.Application.Search;

public sealed class SearchService(ISender mediator) : ISearchService
{
    public Task<Result<SearchResult>> SearchAsync(
        string query, int limit = 10, CancellationToken ct = default)
        => mediator.Send(new SearchProductsQuery(query, PageSize: limit), ct);
}
