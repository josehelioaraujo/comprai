using UcpAgent.SharedKernel.Models;

namespace UcpAgent.SharedKernel.Ports;

public interface ICartPort
{
    Task<string> AddItemAsync(string sessionId, ProductDto product, int quantity, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CartItemDto>> GetItemsAsync(string sessionId, CancellationToken cancellationToken = default);
    Task RemoveItemAsync(string sessionId, string itemId, CancellationToken cancellationToken = default);
    Task ClearAsync(string sessionId, CancellationToken cancellationToken = default);
}

public record CartItemDto(string ItemId, ProductDto Product, int Quantity, decimal Subtotal);
