using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

/// <summary>Stub no-op usado quando UsarPostgres=false.</summary>
public sealed class NullCartSnapshotPort : ICartSnapshotPort
{
    public Task SaveAsync(string sessionId, IReadOnlyList<CartItemDto> items, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<CartSnapshotDto?> GetAsync(string sessionId, CancellationToken ct = default)
        => Task.FromResult<CartSnapshotDto?>(null);

    public Task DeleteAsync(string sessionId, CancellationToken ct = default)
        => Task.CompletedTask;
}
