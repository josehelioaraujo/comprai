using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Mocks;

public sealed class MockCheckoutPort : ICheckoutPort
{
    public Task<CheckoutResultDto> ProcessAsync(string sessionId, CustomerDto customer, CancellationToken cancellationToken = default)
    {
        var orderId = $"MOCK-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        return Task.FromResult(new CheckoutResultDto(orderId, "Confirmed", $"https://comprai.ai/orders/{orderId}"));
    }
}
