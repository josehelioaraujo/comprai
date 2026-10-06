using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UcpAgent.Domain.Fulfillment;
using UcpAgent.Infrastructure.Fulfillment;
using UcpAgent.Infrastructure.Persistence.Repositories;
using UcpAgent.SharedKernel.Ports;
using PostgresFulfillmentRepo = UcpAgent.Infrastructure.Persistence.Repositories.PostgresFulfillmentRepository;

namespace UcpAgent.Infrastructure.Payment;

/// <summary>
/// Decorator de IPaymentPort — persiste pagamento + atualiza order.status +
/// insere fulfillment_event (payment_confirmed) + enfileira simulator.
/// </summary>
public sealed class PersistingPaymentAdapter(
    IPaymentPort inner,
    PaymentRepository paymentRepo,
    OrderRepository orderRepo,
    PostgresFulfillmentRepo fulfillmentRepo,
    IConfiguration configuration,
    ILogger<PersistingPaymentAdapter> logger,
    FulfillmentSimulator? fulfillmentSimulator = null) : IPaymentPort
{
    private readonly bool _usarPostgres =
        string.Equals(configuration["Features:UsarPostgres"], "true",
            StringComparison.OrdinalIgnoreCase);

    public async Task<PaymentResultDto> ProcessAsync(
        string orderId,
        decimal amount,
        string currency,
        PaymentMethodDto method,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.ProcessAsync(orderId, amount, currency, method, cancellationToken);

        if (!result.Success || !_usarPostgres)
            return result;

        try
        {
            // Etapa 4 — INSERT payment + payment_outbox em 1 TX
            var record = new PaymentRecord(
                OrderId:   orderId,
                Provider:  method.Provider,
                Method:    method.CardToken is not null ? "card" : "pix",
                Amount:    amount,
                CardLast4: result.PaymentId?.Length >= 4 ? result.PaymentId[^4..] : null,
                PixKey:    method.PixKey,
                PixQrCode: result.PixQrCode);

            var paymentId = await paymentRepo.ConfirmAsync(record, cancellationToken);
            logger.LogInformation("[F2] Payment persisted: {PaymentId}", paymentId);

            // Etapa 5 — UPDATE order.status: pending → confirmed
            await orderRepo.UpdateStatusAsync(
                orderId,
                Domain.Enums.OrderStatus.Confirmed,
                cancellationToken);
            logger.LogInformation("[F2] Order {OrderId} status → Confirmed", orderId);

            // Etapa 6 — Insere fulfillment_event: payment_confirmed
            var agg = await fulfillmentRepo.GetByOrderIdAsync(orderId, cancellationToken);
            if (agg is null)
            {
                agg = FulfillmentAggregate.Create(orderId);
                await fulfillmentRepo.SaveAsync(agg, cancellationToken);
                logger.LogInformation("[F2] FulfillmentAggregate criado: {OrderId}", orderId);
            }

            // Enqueue simulator para progressão automática
            fulfillmentSimulator?.Enqueue(orderId);
            logger.LogInformation("[F2] Fulfillment enqueued: {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[F2] Falha ao persistir pagamento no BD — continuando");
        }

        return result;
    }
}
