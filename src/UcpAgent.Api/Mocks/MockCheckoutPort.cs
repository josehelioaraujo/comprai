using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Mocks;

public sealed class MockCheckoutPort(
    IOrderPort orders,
    ICartSnapshotPort cartSnapshot) : ICheckoutPort
{
    public async Task<CheckoutResultDto> ProcessAsync(
        string sessionId, CustomerDto customer,
        Guid? authenticatedCustomerId = null,
        CancellationToken ct = default)
    {
        var cart  = await cartSnapshot.GetAsync(sessionId, ct);
        var total = cart?.TotalAmount ?? 0m;

        var orderId = Guid.NewGuid().ToString();
        var dto = new OrderStatusDto(
            OrderId:   orderId,
            Status:    "Pending",
            Total:     total,
            Customer:  customer,
            ItemsJson: null,
            CreatedAt: DateTime.UtcNow);

        await orders.SaveAsync(dto, ct);
        await orders.SaveSessionOrderAsync(sessionId, orderId, ct);

        return new CheckoutResultDto(orderId, true, null);
    }
}
