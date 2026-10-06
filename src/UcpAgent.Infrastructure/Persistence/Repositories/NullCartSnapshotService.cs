using System.Diagnostics.CodeAnalysis;
using UcpAgent.Application.Cart;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

[ExcludeFromCodeCoverage]
public sealed class NullCartSnapshotService : ICartSnapshotService
{
    public Task PersistAsync(string sessionId, ICartPort cart, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task DeleteAsync(string sessionId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<CartSnapshotDto?> GetAsync(string sessionId, CancellationToken ct = default)
        => Task.FromResult<CartSnapshotDto?>(null);
}
