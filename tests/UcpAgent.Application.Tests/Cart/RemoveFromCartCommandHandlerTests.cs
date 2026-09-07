using Bogus;
using FluentAssertions;
using NSubstitute;
using UcpAgent.Application.Cart;
using UcpAgent.SharedKernel.Ports;
using Xunit;

namespace UcpAgent.Application.Tests.Cart;

public class RemoveFromCartCommandHandlerTests
{
    private readonly ICartPort _cart = Substitute.For<ICartPort>();
    private readonly Faker _faker = new("pt_BR");

    [Fact]
    public async Task Handle_DeveRemoverItemERetornarTrue()
    {
        var sessionId = _faker.Random.Guid().ToString();
        var productId = _faker.Random.Guid().ToString();

        var handler = new RemoveFromCartCommandHandler(_cart);
        var result = await handler.Handle(new RemoveFromCartCommand(sessionId, productId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeveChamarPortComParametrosCorretos()
    {
        var sessionId = _faker.Random.Guid().ToString();
        var productId = _faker.Random.Guid().ToString();

        var handler = new RemoveFromCartCommandHandler(_cart);
        await handler.Handle(new RemoveFromCartCommand(sessionId, productId), CancellationToken.None);

        await _cart.Received(1).RemoveItemAsync(sessionId, productId, Arg.Any<CancellationToken>());
    }
}
