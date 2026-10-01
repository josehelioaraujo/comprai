using UcpAgent.Domain.Fulfillment;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Events;

namespace UcpAgent.Application.Fulfillment;

/// <summary>
/// Implementação do serviço de fulfillment.
/// Persiste via IFulfillmentRepository e publica eventos via IEventPublisher (Kafka).
///
/// TODO futuro:
/// - Integrar com API de transportadoras para obter código de rastreio real
/// - Disparar notificações push/email via INotificationPublisher (RabbitMQ)
/// - Implementar SLA monitoring (alertar se pedido travou em um status por muito tempo)
/// </summary>
public sealed class FulfillmentService(
    IFulfillmentRepository repository,
    IEventPublisher events) : IFulfillmentService
{
    public async Task<FulfillmentAggregate> StartAsync(string orderId, CancellationToken ct = default)
    {
        var existing = await repository.GetByOrderIdAsync(orderId, ct);
        if (existing is not null) return existing;   // idempotente

        var agg = FulfillmentAggregate.Create(orderId);
        await repository.SaveAsync(agg, ct);

        await events.PublishAsync(
            UcpTopics.FulfillmentStarted,
            new FulfillmentStatusChangedEvent(
                orderId, FulfillmentStatus.PaymentConfirmed.ToString(),
                "Fulfillment iniciado", DateTime.UtcNow),
            ct);

        return agg;
    }

    public async Task<FulfillmentAggregate> AdvanceAsync(
        string orderId, FulfillmentStatus newStatus, string description,
        string? trackingCode = null, string? carrierCode = null,
        string? location = null, CancellationToken ct = default)
    {
        var agg = await GetOrThrowAsync(orderId, ct);

        agg.AdvanceTo(newStatus, description, trackingCode, carrierCode, location);
        await repository.SaveAsync(agg, ct);

        await events.PublishAsync(
            UcpTopics.FulfillmentStatusChanged,
            new FulfillmentStatusChangedEvent(
                orderId, newStatus.ToString(), description, DateTime.UtcNow,
                trackingCode, carrierCode, location),
            ct);

        return agg;
    }

    public Task<FulfillmentAggregate?> GetAsync(string orderId, CancellationToken ct = default)
        => repository.GetByOrderIdAsync(orderId, ct);

    public Task<IReadOnlyList<FulfillmentEvent>> GetHistoryAsync(string orderId, CancellationToken ct = default)
        => repository.GetHistoryAsync(orderId, ct);

    public async Task<FulfillmentAggregate> CancelAsync(string orderId, string reason, CancellationToken ct = default)
    {
        var agg = await GetOrThrowAsync(orderId, ct);
        agg.Cancel(reason);
        await repository.SaveAsync(agg, ct);

        await events.PublishAsync(
            UcpTopics.FulfillmentCancelled,
            new FulfillmentStatusChangedEvent(
                orderId, FulfillmentStatus.Cancelled.ToString(), reason, DateTime.UtcNow),
            ct);

        return agg;
    }

    private async Task<FulfillmentAggregate> GetOrThrowAsync(string orderId, CancellationToken ct)
        => await repository.GetByOrderIdAsync(orderId, ct)
           ?? throw new KeyNotFoundException($"Fulfillment não encontrado para pedido {orderId}");
}
