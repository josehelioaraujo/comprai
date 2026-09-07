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
    public async Task Handle_SessionComOrder_RetornaOrder()
    {
        var sessionId = _faker.Random.Guid().ToString();
        var orderId = $"ORDER-{_faker.Random.AlphaNumeric(8).ToUpper()}";
        var customer = new CustomerDto(_faker.Name.FullName(), _faker.Internet.Email(), _faker.Phone.PhoneNumber(), _faker.Address.FullAddress());
        var order = new OrderStatusDto(orderId, "Pending", 299.90m, customer, "[]", DateTime.UtcNow);

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
        var customer = new CustomerDto(_faker.Name.FullName(), _faker.Internet.Email(), _faker.Phone.PhoneNumber(), _faker.Address.FullAddress());

        _orders.GetOrderIdBySessionAsync(sessionId, Arg.Any<CancellationToken>()).Returns(orderId);
        _orders.GetStatusAsync(orderId, Arg.Any<CancellationToken>())
               .Returns(new OrderStatusDto(orderId, "Confirmed", 100m, customer, "[]", DateTime.UtcNow));

        var handler = new GetOrderBySessionQueryHandler(_orders);
        await handler.Handle(new GetOrderBySessionQuery(sessionId), CancellationToken.None);

        await _orders.Received(1).GetStatusAsync(orderId, Arg.Any<CancellationToken>());
    }
}
