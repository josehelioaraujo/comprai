using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Mocks;

public sealed class MockOrderPort : IOrderPort
{
    private static readonly string[] _statuses = ["Pending", "Confirmed", "Processing", "Shipped", "Delivered"];

    public Task<OrderStatusDto?> GetStatusAsync(string orderId, CancellationToken cancellationToken = default)
    {
        if (!orderId.StartsWith("MOCK-", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult<OrderStatusDto?>(null);

        var status = _statuses[new Random().Next(_statuses.Length)];
        var tracking = status is "Shipped" or "Delivered" ? $"BR{orderId.Replace("MOCK-", "")}BR" : null;

        return Task.FromResult<OrderStatusDto?>(
            new OrderStatusDto(orderId, status, tracking, DateTime.UtcNow.AddHours(-2)));
    }
}
