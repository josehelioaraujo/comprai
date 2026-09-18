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
    public async Task Handle_DeveRetornarIsSuccessTrue()
    {
        var handler = new RemoveFromCartCommandHandler(_cart);
        var result = await handler.Handle(
            new RemoveFromCartCommand(_faker.Random.Guid().ToString(), _faker.Random.Guid().ToString()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeveRetornarValueTrue()
    {
        var handler = new RemoveFromCartCommandHandler(_cart);
        var result = await handler.Handle(
            new RemoveFromCartCommand(_faker.Random.Guid().ToString(), _faker.Random.Guid().ToString()),
            CancellationToken.None);

        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeveChamarRemoveItemAsyncUmaVez()
    {
        var sessionId = _faker.Random.Guid().ToString();
        var productId = _faker.Random.Guid().ToString();
        var handler = new RemoveFromCartCommandHandler(_cart);

        await handler.Handle(new RemoveFromCartCommand(sessionId, productId), CancellationToken.None);

        await _cart.Received(1).RemoveItemAsync(sessionId, productId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeveChamarPortComSessionIdCorreto()
    {
        var sessionId = "session-especifico-123";
        var productId = _faker.Random.Guid().ToString();
        var handler = new RemoveFromCartCommandHandler(_cart);

        await handler.Handle(new RemoveFromCartCommand(sessionId, productId), CancellationToken.None);

        await _cart.Received(1).RemoveItemAsync(
            Arg.Is<string>(s => s == sessionId),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeveChamarPortComProductIdCorreto()
    {
        var sessionId = _faker.Random.Guid().ToString();
        var productId = "produto-especifico-456";
        var handler = new RemoveFromCartCommandHandler(_cart);

        await handler.Handle(new RemoveFromCartCommand(sessionId, productId), CancellationToken.None);

        await _cart.Received(1).RemoveItemAsync(
            Arg.Any<string>(),
            Arg.Is<string>(p => p == productId),
            Arg.Any<CancellationToken>());
    }
}
