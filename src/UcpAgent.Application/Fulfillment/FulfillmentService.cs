using UcpAgent.Domain.Fulfillment;

namespace UcpAgent.Application.Fulfillment;

/// <summary>
/// Implementação do serviço de fulfillment.
/// A camada Application orquestra as transições — não publica eventos diretamente.
/// A publicação de eventos de domínio fica na Infrastructure (RedisFulfillmentRepository),
/// seguindo o padrão Ports & Adapters correto.
///
/// TODO futuro:
/// - Usar IMediator para publicar domain events via MediatR
/// - Integrar com API de transportadoras para código de rastreio real
/// - Implementar SLA monitoring
/// </summary>
public sealed class FulfillmentService(
    IFulfillmentRepository repository) : IFulfillmentService
{
    public async Task<FulfillmentAggregate> StartAsync(string orderId, CancellationToken ct = default)
    {
        var existing = await repository.GetByOrderIdAsync(orderId, ct);
        if (existing is not null) return existing;   // idempotente

        var agg = FulfillmentAggregate.Create(orderId);
        await repository.SaveAsync(agg, ct);
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
        return agg;
    }

    private async Task<FulfillmentAggregate> GetOrThrowAsync(string orderId, CancellationToken ct)
        => await repository.GetByOrderIdAsync(orderId, ct)
           ?? throw new KeyNotFoundException($"Fulfillment não encontrado para pedido {orderId}");
}
