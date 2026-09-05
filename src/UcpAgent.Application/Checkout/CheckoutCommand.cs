using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Checkout;

public record CheckoutCommand(
    string SessionId,
    CustomerDto Customer
) : IRequest<Result<CheckoutResultDto>>;
