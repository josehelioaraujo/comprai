using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UcpAgent.Domain.Fulfillment;

namespace UcpAgent.Infrastructure.Fulfillment;

/// <summary>
/// Simula a progressão automática do fulfillment para ambiente de demo/dev.
/// Cada pedido avança pelos status com delays configuráveis.
///
/// Em produção: substituir por integrações reais com:
///   - API de WMS (Warehouse Management System)
///   - API de transportadora (Correios, Jadlog, Total Express)
///   - Webhooks de tracking
///
/// Ativado apenas quando Features__UsarFulfillmentSimulator = true no appsettings.
/// </summary>
public sealed class FulfillmentSimulator(
    IFulfillmentService fulfillment,
    ILogger<FulfillmentSimulator> logger) : IHostedService
{
    // Progressão: status → (delay antes deste passo, descrição, trackingCode)
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

    // Fila de pedidos para simular (thread-safe)
    private readonly System.Collections.Concurrent.ConcurrentQueue<string> _queue = new();
    private CancellationTokenSource _cts = new();

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
            {
                _ = Task.Run(() => SimulateAsync(orderId, ct), ct);
            }
            await Task.Delay(500, ct).ConfigureAwait(false);
        }
    }

    private async Task SimulateAsync(string orderId, CancellationToken ct)
    {
        logger.LogInformation("[Fulfillment] Iniciando simulação para pedido {OrderId}", orderId);

        foreach (var (status, delay, description, trackingPattern) in Pipeline)
        {
            await Task.Delay(delay, ct);
            if (ct.IsCancellationRequested) break;

            var tracking = trackingPattern is not null
                ? string.Format(trackingPattern, orderId[..6].ToUpper())
                : null;

            try
            {
                await fulfillment.AdvanceAsync(orderId, status, description,
                    trackingCode: tracking, ct: ct);

                logger.LogInformation(
                    "[Fulfillment] {OrderId} → {Status}: {Description}",
                    orderId, status, description);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[Fulfillment] Erro ao avançar pedido {OrderId} para {Status}",
                    orderId, status);
                break;
            }
        }

        logger.LogInformation("[Fulfillment] Simulação concluída para pedido {OrderId}", orderId);
    }
}
