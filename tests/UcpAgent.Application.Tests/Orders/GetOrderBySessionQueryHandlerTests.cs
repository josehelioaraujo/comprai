using Bogus;
using FluentAssertions;
using NSubstitute;
using UcpAgent.Application.Orders;
using UcpAgent.SharedKernel.Ports;
using Xunit;

namespace UcpAgent.Application.Tests.Orders;

public class GetOrderBySessionQueryHandlerTests
{
    private readonly IOrderPort _orders = Substitute.For<IOrderPort>();
    private readonly Faker _faker = new("pt_BR");

    private OrderStatusDto MakeOrder(string orderId) =>
        new(orderId, "Pending", 299.90m,
            new CustomerDto(_faker.Name.FullName(), _faker.Internet.Email(),
                            _faker.Phone.PhoneNumber(), _faker.Address.FullAddress()),
            "[]", DateTime.UtcNow);

    [Fact]
    public async Task Handle_SessionSemOrder_RetornaNulo()
    {
        var sessionId = _faker.Random.Guid().ToString();
        _orders.GetOrderIdBySessionAsync(sessionId, Arg.Any<CancellationToken>()).Returns((string?)null);
        var handler = new GetOrderBySessionQueryHandler(_orders);

        var result = await handler.Handle(new GetOrderBySessionQuery(sessionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SessionSemOrder_NaoDeveBuscarStatus()
    {
        var sessionId = _faker.Random.Guid().ToString();
        _orders.GetOrderIdBySessionAsync(sessionId, Arg.Any<CancellationToken>()).Returns((string?)null);
        var handler = new GetOrderBySessionQueryHandler(_orders);

        await handler.Handle(new GetOrderBySessionQuery(sessionId), CancellationToken.None);

        await _orders.DidNotReceive().GetStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SessionComOrder_RetornaOrder()
    {
        var sessionId = _faker.Random.Guid().ToString();
        var orderId = $"ORDER-{_faker.Random.AlphaNumeric(8).ToUpper()}";
        var order = MakeOrder(orderId);
        _orders.GetOrderIdBySessionAsync(sessionId, Arg.Any<CancellationToken>()).Returns(orderId);
        _orders.GetStatusAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        var handler = new GetOrderBySessionQueryHandler(_orders);

        var result = await handler.Handle(new GetOrderBySessionQuery(sessionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OrderId.Should().Be(orderId);
        result.Value.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Handle_QuandoOrderIdExiste_DeveBuscarStatusPorId()
    {
        var sessionId = _faker.Random.Guid().ToString();
        var orderId = $"ORDER-{_faker.Random.AlphaNumeric(8).ToUpper()}";
        _orders.GetOrderIdBySessionAsync(sessionId, Arg.Any<CancellationToken>()).Returns(orderId);
        _orders.GetStatusAsync(orderId, Arg.Any<CancellationToken>()).Returns(MakeOrder(orderId));
        var handler = new GetOrderBySessionQueryHandler(_orders);

        await handler.Handle(new GetOrderBySessionQuery(sessionId), CancellationToken.None);

        await _orders.Received(1).GetStatusAsync(orderId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RetornaOrderComStatusCorreto()
    {
        var sessionId = _faker.Random.Guid().ToString();
        var orderId = $"ORDER-{_faker.Random.AlphaNumeric(8).ToUpper()}";
        var customer = new CustomerDto("Helio", "helio@email.com", "11999", "Rua X");
        var order = new OrderStatusDto(orderId, "Confirmed", 500m, customer, "[]", DateTime.UtcNow);
        _orders.GetOrderIdBySessionAsync(sessionId, Arg.Any<CancellationToken>()).Returns(orderId);
        _orders.GetStatusAsync(orderId, Arg.Any<CancellationToken>()).Returns(order);
        var handler = new GetOrderBySessionQueryHandler(_orders);

        var result = await handler.Handle(new GetOrderBySessionQuery(sessionId), CancellationToken.None);

        result.Value!.Status.Should().Be("Confirmed");
        result.Value.Total.Should().Be(500m);
    }
}
