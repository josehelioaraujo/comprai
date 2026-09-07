using UcpAgent.SharedKernel.Ports;
namespace UcpAgent.Api.Mocks;
public sealed class MockOrderPort : IOrderPort
{
    private static readonly string[] Statuses =
        ["Pending", "Confirmed", "Processing", "Shipped", "Delivered"];

    public Task<OrderStatusDto?> GetStatusAsync(string orderId, CancellationToken ct = default)
    {
        if (!orderId.StartsWith("MOCK-") && !orderId.StartsWith("ORDER-"))
            return Task.FromResult<OrderStatusDto?>(null);
        var status = Statuses[Random.Shared.Next(Statuses.Length)];
        var dto = new OrderStatusDto(
            OrderId:   orderId,
            Status:    status,
            Total:     0,
            Customer:  null,
            ItemsJson: null,
            CreatedAt: DateTime.UtcNow);
        return Task.FromResult<OrderStatusDto?>(dto);
    }

    public Task<string?> GetOrderIdBySessionAsync(string sessionId, CancellationToken ct = default)
        => Task.FromResult<string?>(null);
}
