using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UcpAgent.Infrastructure.Persistence.Repositories;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Infrastructure.Payment;

/// <summary>
/// Decorator de IPaymentPort — persiste pagamento + atualiza order.status no BD
/// após o adapter real (Mock/Stripe/Efi) confirmar. Registrado via Decorate quando UsarPostgres=true.
/// </summary>
public sealed class PersistingPaymentAdapter(
    IPaymentPort inner,
    PaymentRepository paymentRepo,
    OrderRepository orderRepo,
    IConfiguration configuration,
    ILogger<PersistingPaymentAdapter> logger) : IPaymentPort
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
            // Etapa 4 — INSERT payment + payment_outbox em 1 TX (com retry+CB via PaymentRepository)
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

            // Etapa 5 — UPDATE order.status: pending → confirmed (com retry+CB via OrderRepository)
            await orderRepo.UpdateStatusAsync(
                orderId,
                Domain.Enums.OrderStatus.Confirmed,
                cancellationToken);
            logger.LogInformation("[F2] Order {OrderId} status → Confirmed", orderId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[F2] Falha ao persistir pagamento no BD — continuando");
        }

        return result;
    }
}
