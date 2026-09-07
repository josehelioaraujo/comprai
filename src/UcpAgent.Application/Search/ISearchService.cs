using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Models;

namespace UcpAgent.Application.Search;

public interface ISearchService
{
    Task<Result<SearchResult>> SearchAsync(string query, int limit = 10, CancellationToken ct = default);
}