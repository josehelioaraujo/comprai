using Microsoft.Extensions.Logging;
using UcpAgent.PriceWatcher.Models;

namespace UcpAgent.PriceWatcher.Channels;

public sealed class NullAlertChannel : IPriceAlertChannel
{
    private readonly ILogger<NullAlertChannel> _logger;
    public NullAlertChannel(ILogger<NullAlertChannel> logger) => _logger = logger;

    public Task PublishAsync(PriceAlertMessage alert, string? email, CancellationToken ct = default)
    {
        _logger.LogInformation("[NullAlertChannel] Alerta ignorado: {Title} novo={NewPrice}", alert.Title, alert.NewPrice);
        return Task.CompletedTask;
    }
}
