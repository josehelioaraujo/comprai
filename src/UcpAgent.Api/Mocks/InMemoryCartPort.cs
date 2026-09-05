using System.Collections.Concurrent;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Mocks;

public sealed class InMemoryCartPort : ICartPort
{
    private readonly ConcurrentDictionary<string, List<CartItemDto>> _carts = new();

    public Task<string> AddItemAsync(string sessionId, ProductDto product, int quantity, CancellationToken cancellationToken = default)
    {
        var items = _carts.GetOrAdd(sessionId, _ => []);

        lock (items)
        {
            var existing = items.FirstOrDefault(i => i.Product.Id == product.Id && i.Product.Source == product.Source);
            if (existing is not null)
            {
                items.Remove(existing);
                var updated = existing with { Quantity = existing.Quantity + quantity, Subtotal = product.Price * (existing.Quantity + quantity) };
                items.Add(updated);
                return Task.FromResult(updated.ItemId);
            }

            var itemId = Guid.NewGuid().ToString("N");
            items.Add(new CartItemDto(itemId, product, quantity, product.Price * quantity));
            return Task.FromResult(itemId);
        }
    }

    public Task<IReadOnlyList<CartItemDto>> GetItemsAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var items = _carts.GetValueOrDefault(sessionId) ?? [];
        return Task.FromResult<IReadOnlyList<CartItemDto>>(items.AsReadOnly());
    }

    public Task RemoveItemAsync(string sessionId, string itemId, CancellationToken cancellationToken = default)
    {
        if (_carts.TryGetValue(sessionId, out var items))
            lock (items) { items.RemoveAll(i => i.ItemId == itemId); }

        return Task.CompletedTask;
    }

    public Task ClearAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _carts.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }
}
