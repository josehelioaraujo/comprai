using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UcpAgent.Infrastructure.Persistence.Repositories;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Payment;

public sealed class ProcessPaymentHandler(
    IPaymentPort payment,
    IConfiguration configuration,
    ILogger<ProcessPaymentHandler> logger,
    PaymentRepository? paymentRepo = null,
    OrderRepository? orderRepo = null)
    : IRequestHandler<ProcessPaymentCommand, Result<PaymentResultDto>>
{
    private readonly bool _usarPostgres =
        string.Equals(configuration["Features:UsarPostgres"], "true",
            StringComparison.OrdinalIgnoreCase);

    public async Task<Result<PaymentResultDto>> Handle(
        ProcessPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var result = await payment.ProcessAsync(
            request.OrderId,
            request.Amount,
            request.Currency,
            request.Method,
            cancellationToken);

        if (!result.Success)
            return Result<PaymentResultDto>.Fail(result.Error ?? "Pagamento recusado");

        // ── F2: Persistência PostgreSQL ──────────────────────────────────
        if (_usarPostgres && paymentRepo is not null && orderRepo is not null)
        {
            try
            {
                // Etapa 4 — INSERT payment + payment_outbox em 1 TX
                var record = new PaymentRecord(
                    OrderId:   request.OrderId,
                    Provider:  request.Method.Provider,
                    Method:    request.Method.CardToken is not null ? "card" : "pix",
                    Amount:    request.Amount,
                    CardLast4: result.PaymentId?.Length >= 4
                                   ? result.PaymentId[^4..] : null,
                    PixKey:    request.Method.PixKey,
                    PixQrCode: result.PixQrCode);

                var paymentId = await paymentRepo.ConfirmAsync(record, cancellationToken);
                logger.LogInformation("[F2] Payment persisted: {PaymentId}", paymentId);

                // Etapa 5 — UPDATE order.status: pending → confirmed (paid)
                await orderRepo.UpdateStatusAsync(
                    request.OrderId,
                    Domain.Enums.OrderStatus.Confirmed,
                    cancellationToken);
                logger.LogInformation("[F2] Order {OrderId} status → Confirmed", request.OrderId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[F2] Falha ao persistir pagamento no BD — continuando");
            }
        }

        return Result<PaymentResultDto>.Ok(result);
    }
}
