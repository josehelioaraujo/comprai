using Bogus;
using FluentAssertions;
using NSubstitute;
using UcpAgent.Application.Order;
using UcpAgent.SharedKernel.Ports;
using Xunit;

namespace UcpAgent.Application.Tests.Orders;

public sealed class GetOrderHandlerTests
{
    private readonly IOrderPort _orders = Substitute.For<IOrderPort>();
    private readonly Faker _faker = new("pt_BR");

    [Fact]
    public async Task Handle_OrderExistente_RetornaStatus()
    {
        var orderId  = $"ORDER-{_faker.Random.AlphaNumeric(8).ToUpper()}";
        var customer = new CustomerDto(_faker.Name.FullName(), _faker.Internet.Email(),
            _faker.Phone.PhoneNumber(), _faker.Address.FullAddress());
        var status   = new OrderStatusDto(orderId, "Confirmed", 350m, customer, "[]", DateTime.UtcNow);

        _orders.GetStatusAsync(orderId, Arg.Any<CancellationToken>()).Returns(status);

        var handler = new GetOrderHandler(_orders);
        var result  = await handler.Handle(new GetOrderQuery(orderId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OrderId.Should().Be(orderId);
        result.Value.Status.Should().Be("Confirmed");
    }

    [Fact]
    public async Task Handle_OrderNaoEncontrado_RetornaNulo()
    {
        _orders.GetStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns((OrderStatusDto?)null);

        var handler = new GetOrderHandler(_orders);
        var result  = await handler.Handle(new GetOrderQuery("ORDER-INEXISTENTE"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DeveChamarPortComOrderIdCorreto()
    {
        var orderId = $"ORDER-{_faker.Random.AlphaNumeric(8).ToUpper()}";
        _orders.GetStatusAsync(orderId, Arg.Any<CancellationToken>()).Returns((OrderStatusDto?)null);

        var handler = new GetOrderHandler(_orders);
        await handler.Handle(new GetOrderQuery(orderId), CancellationToken.None);

        await _orders.Received(1).GetStatusAsync(orderId, Arg.Any<CancellationToken>());
    }
}
