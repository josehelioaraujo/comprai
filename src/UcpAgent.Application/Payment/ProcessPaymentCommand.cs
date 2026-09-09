using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Payment;

public record ProcessPaymentCommand(
    string          OrderId,
    decimal         Amount,
    string          Currency,
    PaymentMethodDto Method
) : IRequest<Result<PaymentResultDto>>;
