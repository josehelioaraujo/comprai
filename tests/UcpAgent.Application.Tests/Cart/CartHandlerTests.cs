using Moq;
using UcpAgent.Application.Cart;
using UcpAgent.SharedKernel;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;
using Xunit;
using FluentAssertions;

namespace UcpAgent.Application.Tests.Cart;

public sealed class CartHandlerTests
{
    private static ProductDto MakeProduct(string id = "P1") =>
        new(id, "Produto Teste", 99.90m, null, null, "eletronicos", "mock");

    private static readonly UcpMetrics _metrics = new();

    // ── AddToCart ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddToCart_ValidProduct_ReturnsItemId()
    {
        var cart    = new Mock<ICartPort>();
        var handler = new AddToCartHandler(cart.Object, _metrics);
        var product = MakeProduct();
        var command = new AddToCartCommand("session-1", product, 2);
        cart.Setup(c => c.AddItemAsync("session-1", product, 2, default))
            .ReturnsAsync("item-abc");

        var result = await handler.Handle(command, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("item-abc");
    }

    [Fact]
    public async Task AddToCart_RetornaExatamenteOItemIdDoPort()
    {
        var cart    = new Mock<ICartPort>();
        var handler = new AddToCartHandler(cart.Object, _metrics);
        var product = MakeProduct();
        var command = new AddToCartCommand("session-1", product, 1);
        cart.Setup(c => c.AddItemAsync(It.IsAny<string>(), It.IsAny<ProductDto>(), It.IsAny<int>(), default))
            .ReturnsAsync("item-especifico-xyz");

        var result = await handler.Handle(command, default);

        result.Value.Should().Be("item-especifico-xyz");
    }

    [Fact]
    public async Task AddToCart_CallsPortWithCorrectArgs()
    {
        var cart    = new Mock<ICartPort>();
        var handler = new AddToCartHandler(cart.Object, _metrics);
        var product = MakeProduct("P99");
        var command = new AddToCartCommand("session-x", product, 3);
        cart.Setup(c => c.AddItemAsync(It.IsAny<string>(), It.IsAny<ProductDto>(), It.IsAny<int>(), default))
            .ReturnsAsync("item-xyz");

        await handler.Handle(command, default);

        cart.Verify(c => c.AddItemAsync("session-x", product, 3, default), Times.Once);
    }

    [Fact]
    public async Task AddToCart_IsSuccessTrue()
    {
        var cart    = new Mock<ICartPort>();
        var handler = new AddToCartHandler(cart.Object, _metrics);
        cart.Setup(c => c.AddItemAsync(It.IsAny<string>(), It.IsAny<ProductDto>(), It.IsAny<int>(), default))
            .ReturnsAsync("item-1");

        var result = await handler.Handle(new AddToCartCommand("s", MakeProduct(), 1), default);

        result.IsSuccess.Should().BeTrue();
    }

    // ── RemoveFromCart ────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveFromCart_ExistingItem_ReturnsSuccess()
    {
        var cart    = new Mock<ICartPort>();
        var handler = new RemoveFromCartCommandHandler(cart.Object);
        cart.Setup(c => c.RemoveItemAsync("session-1", "item-abc", default)).Returns(Task.CompletedTask);

        var result = await handler.Handle(new RemoveFromCartCommand("session-1", "item-abc"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveFromCart_ValueEhSempreTrueNaoFalse()
    {
        var cart    = new Mock<ICartPort>();
        var handler = new RemoveFromCartCommandHandler(cart.Object);
        cart.Setup(c => c.RemoveItemAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);

        var result = await handler.Handle(new RemoveFromCartCommand("s", "p"), default);

        result.Value.Should().Be(true);
        result.Value.Should().NotBe(false);
    }

    [Fact]
    public async Task RemoveFromCart_CallsPortWithCorrectArgs()
    {
        var cart    = new Mock<ICartPort>();
        var handler = new RemoveFromCartCommandHandler(cart.Object);
        cart.Setup(c => c.RemoveItemAsync(It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);

        await handler.Handle(new RemoveFromCartCommand("session-2", "item-xyz"), default);

        cart.Verify(c => c.RemoveItemAsync("session-2", "item-xyz", default), Times.Once);
    }
}
