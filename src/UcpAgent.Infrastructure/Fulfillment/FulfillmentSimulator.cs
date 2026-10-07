using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UcpAgent.Domain.Fulfillment;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Fulfillment;

/// <summary>
/// Simula a progressão automática do fulfillment para ambiente de demo/dev.
/// Ao atingir status final (Delivered / Cancelled / Returned),
/// insere registro em order_history como source of truth histórico.
/// </summary>
public sealed class FulfillmentSimulator(
    IFulfillmentRepository repository,
    ILogger<FulfillmentSimulator> logger,
    IOrderHistoryPort? orderHistory = null) : IHostedService
{
    private static readonly (FulfillmentStatus Status, TimeSpan Delay, string Description, string? Tracking)[]
        Pipeline =
        [
            (FulfillmentStatus.Preparing,       TimeSpan.FromSeconds(8),  "Separando e embalando os itens",          null),
            (FulfillmentStatus.ReadyToShip,     TimeSpan.FromSeconds(6),  "Embalado — aguardando coleta",            null),
            (FulfillmentStatus.HandedToCarrier, TimeSpan.FromSeconds(5),  "Coletado pela transportadora",            "BR{0}00001"),
            (FulfillmentStatus.InTransit,       TimeSpan.FromSeconds(8),  "Em trânsito — Centro de Distribuição SP", null),
            (FulfillmentStatus.OutForDelivery,  TimeSpan.FromSeconds(6),  "Saiu para entrega",                       null),
            (FulfillmentStatus.Delivered,       TimeSpan.FromSeconds(5),  "Entregue ao destinatário",                null),
        ];

    private readonly System.Collections.Concurrent.ConcurrentQueue<string> _queue = new();
    private readonly CancellationTokenSource _cts = new();

    public Task StartAsync(CancellationToken ct)
    {
        _ = Task.Run(() => ProcessQueueAsync(_cts.Token), ct);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        _cts.Cancel();
        return Task.CompletedTask;
    }

    /// <summary>Enfileira um pedido para simulação após confirmação de pagamento.</summary>
    public void Enqueue(string orderId) => _queue.Enqueue(orderId);

    private async Task ProcessQueueAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var orderId))
                _ = Task.Run(() => SimulateAsync(orderId, ct), ct);
            await Task.Delay(500, ct).ConfigureAwait(false);
        }
    }

    private async Task SimulateAsync(string orderId, CancellationToken ct)
    {
        logger.LogInformation("[Fulfillment] Iniciando simulação para pedido {OrderId}", orderId);

        var agg = await repository.GetByOrderIdAsync(orderId, ct);
        if (agg is null)
        {
            agg = FulfillmentAggregate.Create(orderId);
            await repository.SaveAsync(agg, ct);
        }

        FulfillmentStatus finalStatus = FulfillmentStatus.Delivered;

        foreach (var (status, delay, description, trackingPattern) in Pipeline)
        {
            await Task.Delay(delay, ct);
            if (ct.IsCancellationRequested) break;

            var tracking = trackingPattern is not null
                ? string.Format(trackingPattern, orderId[..6].ToUpper()) : null;
            try
            {
                agg.AdvanceTo(status, description, tracking);
                await repository.SaveAsync(agg, ct);
                logger.LogInformation("[Fulfillment] {OrderId} → {Status}", orderId, status);
                finalStatus = status;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Fulfillment] Erro ao avançar {OrderId} para {Status}", orderId, status);
                break;
            }
        }

        // INSERT order_history ao atingir status final
        var isFinal = finalStatus is FulfillmentStatus.Delivered
                                  or FulfillmentStatus.Cancelled
                                  or FulfillmentStatus.Returned;

        if (isFinal && orderHistory is not null)
        {
            try
            {
                var itemsJson = JsonSerializer.Serialize(
                    agg.Events.Select(e => new { status = e.Status.ToString(), e.Description, e.OccurredAt }));

                await orderHistory.InsertAsync(new OrderHistoryEntry(
                    OrderId:       orderId,
                    CustomerId:    null,       // sem customer_id no simulator (preenchido via order)
                    Status:        finalStatus.ToString().ToLower(),
                    TotalAmount:   0m,         // valor real na tabela order — aqui é apenas registro histórico
                    ItemsSnapshot: itemsJson), ct);

                logger.LogInformation("[Fulfillment] order_history inserido: {OrderId} → {Status}",
                    orderId, finalStatus);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Fulfillment] Falha ao inserir order_history para {OrderId}", orderId);
            }
        }

        logger.LogInformation("[Fulfillment] Simulação concluída para {OrderId}", orderId);
    }
}
