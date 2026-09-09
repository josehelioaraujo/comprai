using Bogus;
using FluentAssertions;
using MediatR;
using NSubstitute;
using UcpAgent.Application.Orders;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Ports;
using Xunit;

namespace UcpAgent.Application.Tests.Orders;

public sealed class OrderServiceTests
{
    private readonly ISender _mediator = Substitute.For<ISender>();
    private readonly Faker _faker = new("pt_BR");

    [Fact]
    public async Task GetOrderBySession_SessaoComOrder_RetornaOrder()
    {
        var sessionId = _faker.Random.Guid().ToString();
        var orderId = $"ORDER-{_faker.Random.AlphaNumeric(8).ToUpper()}";
        var customer = new CustomerDto(_faker.Name.FullName(), _faker.Internet.Email(),
            _faker.Phone.PhoneNumber(), _faker.Address.FullAddress());
        var orderStatus = new OrderStatusDto(orderId, "Pending", 299.90m, customer, "[]", DateTime.UtcNow);
        var expected = Result<OrderStatusDto?>.Ok(orderStatus);

        _mediator.Send(Arg.Any<GetOrderBySessionQuery>(), Arg.Any<CancellationToken>())
                 .Returns(expected);

        var service = new OrderService(_mediator);
        var result = await service.GetOrderBySessionAsync(sessionId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task GetOrderBySession_SessaoSemOrder_RetornaNulo()
    {
        var sessionId = _faker.Random.Guid().ToString();
        var expected = Result<OrderStatusDto?>.Ok(null);

        _mediator.Send(Arg.Any<GetOrderBySessionQuery>(), Arg.Any<CancellationToken>())
                 .Returns(expected);

        var service = new OrderService(_mediator);
        var result = await service.GetOrderBySessionAsync(sessionId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetOrderBySession_DeveChamarMediatorComQueryCorreta()
    {
        var sessionId = _faker.Random.Guid().ToString();

        _mediator.Send(Arg.Any<GetOrderBySessionQuery>(), Arg.Any<CancellationToken>())
                 .Returns(Result<OrderStatusDto?>.Ok(null));

        var service = new OrderService(_mediator);
        await service.GetOrderBySessionAsync(sessionId);

        await _mediator.Received(1).Send(
            Arg.Is<GetOrderBySessionQuery>(q => q.SessionId == sessionId),
            Arg.Any<CancellationToken>());
    }
}
