using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

/// <summary>Stub no-op usado quando UsarPostgres=false — permite injeção do endpoint sem PostgreSQL.</summary>
public sealed class NullWebhookEventPort : IWebhookEventPort
{
    public Task<bool> TryRecordAsync(string provider, string externalId, string eventType,
        string payload, CancellationToken ct = default)
        => Task.FromResult(true); // sempre "novo" — sem deduplicação
}
