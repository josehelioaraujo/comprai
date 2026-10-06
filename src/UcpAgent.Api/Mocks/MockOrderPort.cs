using System.Collections.Concurrent;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Mocks;

public sealed class MockOrderPort : IOrderPort
{
    private static readonly ConcurrentDictionary<string, OrderStatusDto> _store = new();
    private static readonly ConcurrentDictionary<string, string> _sessionStore  = new();

    public Task<OrderStatusDto?> GetStatusAsync(string orderId, CancellationToken ct = default)
    {
        _store.TryGetValue(orderId, out var dto);
        dto ??= new OrderStatusDto(
            OrderId:   orderId,
            Status:    "Pending",
            Total:     0,
            Customer:  new CustomerDto("", "", "", ""),
            ItemsJson: "[]",
            CreatedAt: DateTime.UtcNow);
        return Task.FromResult<OrderStatusDto?>(dto);
    }

    public Task<string?> GetOrderIdBySessionAsync(string sessionId, CancellationToken ct = default)
    {
        _sessionStore.TryGetValue(sessionId, out var orderId);
        return Task.FromResult(orderId);
    }

    public Task SaveAsync(OrderStatusDto order, CancellationToken ct = default)
    {
        _store[order.OrderId] = order;
        return Task.CompletedTask;
    }

    public Task SaveSessionOrderAsync(string sessionId, string orderId, CancellationToken ct = default)
    {
        _sessionStore[sessionId] = orderId;
        return Task.CompletedTask;
    }
}
