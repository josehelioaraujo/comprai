using UcpAgent.SharedKernel.Models;
using Bogus;
using FluentAssertions;
using NSubstitute;
using UcpAgent.Application.Cart;
using UcpAgent.SharedKernel.Ports;
using Xunit;

namespace UcpAgent.Application.Tests.Cart;

public class GetCartQueryHandlerTests
{
    private readonly ICartPort _cart = Substitute.For<ICartPort>();
    private readonly Faker _faker = new("pt_BR");

    [Fact]
    public async Task Handle_CarrinhoComItens_RetornaItens()
    {
        var sessionId = _faker.Random.Guid().ToString();
        var product = new ProductDto("p1", _faker.Commerce.ProductName(), _faker.Random.Decimal(10, 500), null, null, null, "mock");
        var items = new List<CartItemDto> { new("item-1", product, 2, product.Price * 2) };
        _cart.GetItemsAsync(sessionId, Arg.Any<CancellationToken>()).Returns(items);

        var handler = new GetCartQueryHandler(_cart);
        var result = await handler.Handle(new GetCartQuery(sessionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].Quantity.Should().Be(2);
    }

    [Fact]
    public async Task Handle_CarrinhoVazio_RetornaListaVazia()
    {
        var sessionId = _faker.Random.Guid().ToString();
        _cart.GetItemsAsync(sessionId, Arg.Any<CancellationToken>()).Returns(new List<CartItemDto>());

        var handler = new GetCartQueryHandler(_cart);
        var result = await handler.Handle(new GetCartQuery(sessionId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DeveChamarPortComSessionIdCorreto()
    {
        var sessionId = _faker.Random.Guid().ToString();
        _cart.GetItemsAsync(sessionId, Arg.Any<CancellationToken>()).Returns(new List<CartItemDto>());

        var handler = new GetCartQueryHandler(_cart);
        await handler.Handle(new GetCartQuery(sessionId), CancellationToken.None);

        await _cart.Received(1).GetItemsAsync(sessionId, Arg.Any<CancellationToken>());
    }
}

