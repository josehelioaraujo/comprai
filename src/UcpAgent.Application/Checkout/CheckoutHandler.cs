using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Checkout;

public sealed class CheckoutHandler(ICheckoutPort checkout)
    : IRequestHandler<CheckoutCommand, Result<CheckoutResultDto>>
{
    public async Task<Result<CheckoutResultDto>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        var result = await checkout.ProcessAsync(request.SessionId, request.Customer, cancellationToken);
        return Result<CheckoutResultDto>.Ok(result);
    }
}
