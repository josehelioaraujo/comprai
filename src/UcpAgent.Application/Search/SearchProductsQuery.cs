using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Models;

namespace UcpAgent.Application.Search;

public sealed record SearchProductsQuery(
    string   Query,
    int      Page      = 1,
    int      PageSize  = 10,
    string?  Category  = null,
    decimal? MinPrice  = null,
    decimal? MaxPrice  = null,
    string?  SessionId = null)
    : IRequest<Result<SearchResult>>;