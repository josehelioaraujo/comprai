using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Persistence.Repositories;

/// <summary>Stub no-op usado quando UsarPostgres=false — permite injeção do endpoint sem PostgreSQL.</summary>
public sealed class NullCartSnapshotPort : ICartSnapshotPort
{
    public Task SaveAsync(string sessionId, IReadOnlyList<CartItemDto> items, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<CartItemDto>?> GetAsync(string sessionId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CartItemDto>?>(null);

    public Task DeleteAsync(string sessionId, CancellationToken ct = default)
        => Task.CompletedTask;
}
