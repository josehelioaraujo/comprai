using UcpAgent.SharedKernel;

namespace UcpAgent.Application.Cart;

public interface ICartService
{
    Task<Result<object>> GetCartAsync(string sessionId, CancellationToken ct = default);
    Task<Result<object>> AddItemAsync(string sessionId, string productId, int quantity, CancellationToken ct = default);
    Task<Result<object>> RemoveItemAsync(string sessionId, string productId, CancellationToken ct = default);
}
