using System.Diagnostics;
using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Search;

public sealed class SearchProductsHandler(
    IEnumerable<IProductCatalogPort> catalogs,
    UcpMetrics metrics)
    : IRequestHandler<SearchProductsQuery, Result<SearchResult>>
{
    public async Task<Result<SearchResult>> Handle(
        SearchProductsQuery request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        metrics.SearchTotal.Add(1);

        var searchRequest = new SearchRequest(
            request.Query, request.Page, request.PageSize,
            request.Category, request.MinPrice, request.MaxPrice);

        // Fan-out paralelo — falhas individuais não derrubam a busca
        var tasks = catalogs.Select(async c =>
        {
            var pluginSw = Stopwatch.StartNew();
            metrics.PluginSearchTotal.Add(1, new KeyValuePair<string, object?>("plugin", c.SourceName));
            try
            {
                var result = await c.SearchAsync(searchRequest, cancellationToken);
                pluginSw.Stop();
                metrics.PluginDurationMs.Record(pluginSw.Elapsed.TotalMilliseconds,
                    new KeyValuePair<string, object?>("plugin", c.SourceName));

                if (result.Items.Count == 0)
                    metrics.PluginFallbackTotal.Add(1, new KeyValuePair<string, object?>("plugin", c.SourceName));

                return result;
            }
            catch
            {
                pluginSw.Stop();
                metrics.PluginErrorTotal.Add(1, new KeyValuePair<string, object?>("plugin", c.SourceName));
                metrics.PluginFallbackTotal.Add(1, new KeyValuePair<string, object?>("plugin", c.SourceName));
                return new SearchResult([], 0, request.Page, request.PageSize, c.SourceName);
            }
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

        sw.Stop();
        metrics.SearchDurationMs.Record(sw.Elapsed.TotalMilliseconds);
        metrics.SearchResultsCount.Record(items.Count);

        return Result<SearchResult>.Ok(
            new SearchResult(items, total, request.Page, request.PageSize, "aggregated"));
    }
}
