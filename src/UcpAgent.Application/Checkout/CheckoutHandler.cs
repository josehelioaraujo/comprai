using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Checkout;

public sealed class CheckoutHandler(ICheckoutPort checkout, UcpMetrics metrics)
    : IRequestHandler<CheckoutCommand, Result<CheckoutResultDto>>
{
    public async Task<Result<CheckoutResultDto>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        metrics.CheckoutTotal.Add(1);
        try
        {
            var result = await checkout.ProcessAsync(
                request.SessionId, request.Customer, request.CustomerId, cancellationToken);

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
        catch (Exception ex)
        {
            metrics.CheckoutFailureTotal.Add(1);
            return Result<CheckoutResultDto>.Fail(ex.Message);
        }
    }
}
