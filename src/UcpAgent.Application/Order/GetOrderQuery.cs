using MediatR;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Application.Order;

public record GetOrderQuery(string OrderId) : IRequest<Result<OrderStatusDto?>>;
