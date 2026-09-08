using UcpAgent.PriceWatcher.Models;

namespace UcpAgent.PriceWatcher;

public interface IPriceAlertChannel
{
    Task PublishAsync(PriceAlertMessage alert, string? email, CancellationToken ct = default);
}
