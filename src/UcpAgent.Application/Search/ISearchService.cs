using UcpAgent.SharedKernel;

namespace UcpAgent.Application.Search;

public interface ISearchService
{
    Task<Result<IReadOnlyList<object>>> SearchAsync(string query, int limit = 10, CancellationToken ct = default);
}
