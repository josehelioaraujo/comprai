using UcpAgent.SharedKernel.Events;
using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Search;

public sealed class SearchProductsHandler(IEnumerable<IProductCatalogPort> catalogs, IEventPublisher events)
    : IRequestHandler<SearchProductsQuery, Result<SearchResult>>
{
    public async Task<Result<SearchResult>> Handle(
        SearchProductsQuery request, CancellationToken cancellationToken)
    {
        var searchRequest = new SearchRequest(
            request.Query, request.Page, request.PageSize,
            request.Category, request.MinPrice, request.MaxPrice);

        // Fan-out paralelo — falhas individuais não derrubam a busca
        var tasks = catalogs.Select(async c =>
        {
            try   { return await c.SearchAsync(searchRequest, cancellationToken); }
            catch { return new SearchResult([], 0, request.Page, request.PageSize, c.SourceName); }
        });

        var results = await Task.WhenAll(tasks);

        // Agrega, deduplica por (Id+Source) e rankeia
        var seen  = new HashSet<string>();
        var items = results
            .SelectMany(r => r.Items)
            .Where(p => seen.Add($"{p.Source}:{p.Id}"))   // deduplicação
            .OrderByDescending(p => p.AvailableQuantity > 0) // disponíveis primeiro
            .ThenBy(p => p.Price)                            // menor preço
            .ToList();

        var total = results.Sum(r => r.TotalItems);

        return Result<SearchResult>.Ok(
            new SearchResult(items, total, request.Page, request.PageSize, "aggregated"));
    }
}

