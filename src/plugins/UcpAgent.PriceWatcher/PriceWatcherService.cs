using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UcpAgent.PriceWatcher.Hubs;
using UcpAgent.PriceWatcher.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.PriceWatcher;

public sealed class PriceWatcherService : BackgroundService
{
    private readonly ConcurrentDictionary<string, PriceWatch> _watches = new();
    private readonly IProductCatalogPort                      _catalog;
    private readonly IHubContext<PriceHub>                    _hub;
    private readonly IPriceAlertChannel                       _channel;
    private readonly ILogger<PriceWatcherService>             _logger;

    public PriceWatcherService(
        IProductCatalogPort catalog,
        IHubContext<PriceHub> hub,
        IPriceAlertChannel channel,
        ILogger<PriceWatcherService> logger)
    {
        _catalog = catalog;
        _hub     = hub;
        _channel = channel;
        _logger  = logger;
    }

    public string AddWatch(string sessionId, PriceWatchRequest req)
    {
        var watchId = Guid.NewGuid().ToString("N")[..8];
        var watch   = new PriceWatch(
            watchId, sessionId,
            req.ProductId, req.Title,
            req.CurrentPrice, req.TargetPrice,
            req.ImageUrl, req.Email,
            DateTime.UtcNow);
        _watches[watchId] = watch;
        _logger.LogInformation("PriceWatch {WatchId} adicionado: {Title} alvo={TargetPrice}", watchId, req.Title, req.TargetPrice);
        return watchId;
    }

    public IEnumerable<PriceWatch> GetWatches(string sessionId) =>
        _watches.Values.Where(w => w.SessionId == sessionId);

    public async Task<bool> TriggerTestAsync(string sessionId, string watchId, CancellationToken ct = default)
    {
        if (!_watches.TryGetValue(watchId, out var watch) || watch.SessionId != sessionId)
            return false;

        var alert = new PriceAlertMessage(
            watch.WatchId, watch.SessionId,
            watch.ProductId, watch.Title,
            watch.CurrentPrice,
            watch.TargetPrice - 1m,
            watch.TargetPrice,
            watch.ImageUrl,
            DateTime.UtcNow);

        await _hub.Clients.Group(sessionId).SendAsync("PriceAlert", alert, ct);
        await _channel.PublishAsync(alert, watch.Email, ct);
        _watches.TryRemove(watchId, out _);
        _logger.LogInformation("[TriggerTest] Alerta manual disparado: {WatchId}", watchId);
        return true;
    }

    public bool RemoveWatch(string sessionId, string watchId) =>
        _watches.TryGetValue(watchId, out var w) && w.SessionId == sessionId && _watches.TryRemove(watchId, out _);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("PriceWatcherService iniciado");
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), ct);
            await CheckPricesAsync(ct);
        }
    }

    private async Task CheckPricesAsync(CancellationToken ct)
    {
        foreach (var watch in _watches.Values.ToList())
        {
            try
            {
                var results = await _catalog.SearchAsync(
                    new SharedKernel.Models.SearchRequest(watch.ProductId, 1, 1), ct);

                var product = results.Items.FirstOrDefault();
                if (product is null) continue;

                if (product.Price <= watch.TargetPrice)
                {
                    var alert = new PriceAlertMessage(
                        watch.WatchId, watch.SessionId,
                        watch.ProductId, watch.Title,
                        watch.CurrentPrice, product.Price,
                        watch.TargetPrice, watch.ImageUrl,
                        DateTime.UtcNow);

                    await _hub.Clients
                        .Group(watch.SessionId)
                        .SendAsync("PriceAlert", alert, ct);

                    await _channel.PublishAsync(alert, watch.Email, ct);

                    _watches.TryRemove(watch.WatchId, out _);
                    _logger.LogInformation("Alerta disparado: {Title} novo preço={NewPrice}", watch.Title, product.Price);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao verificar preço de {WatchId}", watch.WatchId);
            }
        }
    }
}
