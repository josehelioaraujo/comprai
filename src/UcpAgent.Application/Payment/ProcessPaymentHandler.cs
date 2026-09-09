using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Payment;

public sealed class ProcessPaymentHandler(IPaymentPort payment)
    : IRequestHandler<ProcessPaymentCommand, Result<PaymentResultDto>>
{
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

        return result.Success
            ? Result<PaymentResultDto>.Ok(result)
            : Result<PaymentResultDto>.Fail(result.Error ?? "Pagamento recusado");
    }
}
