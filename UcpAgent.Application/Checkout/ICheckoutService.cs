using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Checkout;

public interface ICheckoutService
{
    Task<Result<CheckoutResultDto>> CheckoutAsync(
        string sessionId, CustomerDto customer, CancellationToken ct = default);
}
