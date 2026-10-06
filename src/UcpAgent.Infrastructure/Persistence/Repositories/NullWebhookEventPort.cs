using System.Diagnostics.CodeAnalysis;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

[ExcludeFromCodeCoverage]
public sealed class NullWebhookEventPort : IWebhookEventPort
{
    public Task<bool> TryRecordAsync(string provider, string externalId, string eventType,
        string payload, CancellationToken ct = default)
        => Task.FromResult(true);
}
