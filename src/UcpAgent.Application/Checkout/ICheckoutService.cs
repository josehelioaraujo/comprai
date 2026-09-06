using UcpAgent.SharedKernel;

namespace UcpAgent.Application.Checkout;

public interface ICheckoutService
{
    Task<Result<object>> CheckoutAsync(string sessionId, CancellationToken ct = default);
}
