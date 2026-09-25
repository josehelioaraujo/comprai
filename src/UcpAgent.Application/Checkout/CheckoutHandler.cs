using MediatR;
using UcpAgent.Api;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Checkout;

public sealed class CheckoutHandler(ICheckoutPort checkout, UcpMetrics metrics)
    : IRequestHandler<CheckoutCommand, Result<CheckoutResultDto>>
{
    public async Task<Result<CheckoutResultDto>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        metrics.CheckoutTotal.Add(1);
        var result = await checkout.ProcessAsync(request.SessionId, request.Customer, cancellationToken);

        if (result.Success)
        {
            metrics.CheckoutSuccessTotal.Add(1);
            metrics.OrderPlacedTotal.Add(1);
        }
        else
        {
            metrics.CheckoutFailureTotal.Add(1);
        }

        return Result<CheckoutResultDto>.Ok(result);
    }
}
