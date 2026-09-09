using Moq;
using UcpAgent.Application.Cart;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;
using Xunit;

namespace UcpAgent.Application.Tests.Cart;

public sealed class CartHandlerTests
{
    private static ProductDto MakeProduct(string id = "P1") =>
        new(id, "Produto Teste", 99.90m, null, null, "eletronicos", "mock");

    // ── AddToCart ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddToCart_ValidProduct_ReturnsItemId()
    {
        // Arrange
        var cart    = new Mock<ICartPort>();
        var handler = new AddToCartHandler(cart.Object);
        var product = MakeProduct();
        var command = new AddToCartCommand("session-1", product, 2);

        cart.Setup(c => c.AddItemAsync("session-1", product, 2, default))
            .ReturnsAsync("item-abc");

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("item-abc", result.Value);
    }

    [Fact]
    public async Task AddToCart_CallsPortWithCorrectArgs()
    {
        // Arrange
        var cart    = new Mock<ICartPort>();
        var handler = new AddToCartHandler(cart.Object);
        var product = MakeProduct("P99");
        var command = new AddToCartCommand("session-x", product, 3);

        cart.Setup(c => c.AddItemAsync(It.IsAny<string>(), It.IsAny<ProductDto>(), It.IsAny<int>(), default))
            .ReturnsAsync("item-xyz");

        // Act
        await handler.Handle(command, default);

        // Assert
        cart.Verify(c => c.AddItemAsync("session-x", product, 3, default), Times.Once);
    }

    // ── RemoveFromCart ────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveFromCart_ExistingItem_ReturnsSuccess()
    {
        // Arrange
        var cart    = new Mock<ICartPort>();
        var handler = new RemoveFromCartHandler(cart.Object);
        var command = new RemoveFromCartCommand("session-1", "item-abc");

        cart.Setup(c => c.RemoveItemAsync("session-1", "item-abc", default))
            .Returns(Task.CompletedTask);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task RemoveFromCart_CallsPortWithCorrectArgs()
    {
        // Arrange
        var cart    = new Mock<ICartPort>();
        var handler = new RemoveFromCartHandler(cart.Object);
        var command = new RemoveFromCartCommand("session-2", "item-xyz");

        cart.Setup(c => c.RemoveItemAsync(It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        // Act
        await handler.Handle(command, default);

        // Assert
        cart.Verify(c => c.RemoveItemAsync("session-2", "item-xyz", default), Times.Once);
    }
}
