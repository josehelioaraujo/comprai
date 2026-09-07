using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Cart;

public record GetCartQuery(string SessionId) : IRequest<Result<IReadOnlyList<CartItemDto>>>;