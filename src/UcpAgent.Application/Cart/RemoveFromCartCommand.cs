using MediatR;
using UcpAgent.SharedKernel;

namespace UcpAgent.Application.Cart;

public record RemoveFromCartCommand(
    string SessionId,
    string ProductId
) : IRequest<Result<bool>>;