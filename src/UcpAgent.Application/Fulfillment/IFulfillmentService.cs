using UcpAgent.Domain.Fulfillment;

namespace UcpAgent.Application.Fulfillment;

/// <summary>
/// Contrato do serviço de fulfillment.
/// Orquestra as transições de status e dispara eventos de domínio.
/// </summary>
public interface IFulfillmentService
{
    /// <summary>Inicia o fulfillment após pagamento confirmado.</summary>
    Task<FulfillmentAggregate> StartAsync(string orderId, CancellationToken ct = default);

    /// <summary>Avança o status para o próximo estágio do fulfillment.</summary>
    Task<FulfillmentAggregate> AdvanceAsync(
        string orderId, FulfillmentStatus newStatus,
        string description,
        string? trackingCode = null,
        string? carrierCode  = null,
        string? location     = null,
        CancellationToken ct = default);

    /// <summary>Retorna o estado atual do fulfillment.</summary>
    Task<FulfillmentAggregate?> GetAsync(string orderId, CancellationToken ct = default);

    /// <summary>Retorna o histórico completo de eventos.</summary>
    Task<IReadOnlyList<FulfillmentEvent>> GetHistoryAsync(string orderId, CancellationToken ct = default);

    /// <summary>Cancela o fulfillment com motivo.</summary>
    Task<FulfillmentAggregate> CancelAsync(string orderId, string reason, CancellationToken ct = default);
}
