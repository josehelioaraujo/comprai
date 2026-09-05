using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Models;

namespace UcpAgent.Application.Search;

public record SearchProductsQuery(
    string Query,
    int Page = 1,
    int PageSize = 20,
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null
) : IRequest<Result<IReadOnlyList<SearchResult>>>;
