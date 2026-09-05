using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Models;

namespace UcpAgent.Application.Cart;

public record AddToCartCommand(
    string SessionId,
    ProductDto Product,
    int Quantity = 1
) : IRequest<Result<string>>;
