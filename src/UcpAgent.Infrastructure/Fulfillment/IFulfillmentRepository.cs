using UcpAgent.Domain.Fulfillment;

namespace UcpAgent.Infrastructure.Fulfillment;

/// <summary>
/// Porta de persistência do agregado de Fulfillment.
/// Implementações: RedisFulfillmentRepository (cache/dev) e
/// PostgresFulfillmentRepository (produção).
/// </summary>
public interface IFulfillmentRepository
{
    Task<FulfillmentAggregate?> GetByOrderIdAsync(string orderId, CancellationToken ct = default);
    Task SaveAsync(FulfillmentAggregate aggregate, CancellationToken ct = default);
    Task<IReadOnlyList<FulfillmentEvent>> GetHistoryAsync(string orderId, CancellationToken ct = default);
    Task<IReadOnlyList<FulfillmentAggregate>> ListByStatusAsync(FulfillmentStatus status, CancellationToken ct = default);
}
