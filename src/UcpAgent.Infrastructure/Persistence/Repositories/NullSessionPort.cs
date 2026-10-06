using System.Diagnostics.CodeAnalysis;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

/// <summary>Stub no-op para quando UsarPostgres=false.</summary>
[ExcludeFromCodeCoverage]
public sealed class NullSessionPort : ISessionPort
{
    public Task<Guid> GetOrCreateAsync(string? clientSessionId, string channel = "web",
        CancellationToken ct = default)
        => Task.FromResult(Guid.TryParse(clientSessionId, out var g) ? g : Guid.NewGuid());

    public Task LinkCustomerAsync(Guid sessionId, Guid customerId,
        CancellationToken ct = default)
        => Task.CompletedTask;
}
