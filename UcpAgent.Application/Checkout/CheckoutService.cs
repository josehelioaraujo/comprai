using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Checkout;

public sealed class CheckoutService(ISender mediator) : ICheckoutService
{
    public Task<Result<CheckoutResultDto>> CheckoutAsync(
        string sessionId, CustomerDto customer, CancellationToken ct = default)
        => mediator.Send(new CheckoutCommand(sessionId, customer), ct);
}
