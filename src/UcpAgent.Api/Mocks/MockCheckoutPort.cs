using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Mocks;

public sealed class MockCheckoutPort : ICheckoutPort
{
    public Task<CheckoutResultDto> ProcessAsync(
        string sessionId, CustomerDto customer, CancellationToken ct = default)
    {
        // UUID puro — compatível com order.id (UUID) no PostgreSQL
        var orderId = Guid.NewGuid().ToString();
        return Task.FromResult(new CheckoutResultDto(orderId, true, null));
    }
}
