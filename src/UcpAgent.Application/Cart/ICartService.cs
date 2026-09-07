using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Cart;

public interface ICartService
{
    Task<Result<IReadOnlyList<CartItemDto>>> GetCartAsync(string sessionId, CancellationToken ct = default);
    Task<Result<string>> AddItemAsync(string sessionId, ProductDto product, int quantity = 1, CancellationToken ct = default);
    Task<Result<bool>> RemoveItemAsync(string sessionId, string productId, CancellationToken ct = default);
}