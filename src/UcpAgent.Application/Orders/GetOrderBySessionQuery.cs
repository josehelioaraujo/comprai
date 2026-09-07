using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Orders;

public record GetOrderBySessionQuery(string SessionId) : IRequest<Result<OrderStatusDto?>>;