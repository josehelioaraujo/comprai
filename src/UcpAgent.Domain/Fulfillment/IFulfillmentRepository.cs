namespace UcpAgent.Domain.Fulfillment;

/// <summary>
/// Contrato de persistência do Fulfillment.
/// Implementações possíveis:
///   - RedisFulfillmentRepository  → atual (demo/dev)
///   - PostgresFulfillmentRepository → produção (EF Core + append-only)
///   - MongoFulfillmentRepository    → produção (document store)
/// </summary>
public interface IFulfillmentRepository
{
    /// <summary>Busca o estado atual do fulfillment de um pedido.</summary>
    Task<FulfillmentAggregate?> GetByOrderIdAsync(string orderId, CancellationToken ct = default);

    /// <summary>Persiste o agregado (estado + todos os eventos).</summary>
    Task SaveAsync(FulfillmentAggregate aggregate, CancellationToken ct = default);

    /// <summary>Retorna o histórico completo de eventos de um pedido.</summary>
    Task<IReadOnlyList<FulfillmentEvent>> GetHistoryAsync(string orderId, CancellationToken ct = default);

    /// <summary>
    /// Lista todos os fulfillments em um determinado status.
    /// Útil para dashboards operacionais e alertas de SLA.
    /// </summary>
    Task<IReadOnlyList<FulfillmentAggregate>> ListByStatusAsync(
        FulfillmentStatus status, CancellationToken ct = default);
}
