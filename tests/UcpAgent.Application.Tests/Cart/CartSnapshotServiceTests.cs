using Moq;
using UcpAgent.Application.Cart;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;
using Xunit;
using FluentAssertions;

namespace UcpAgent.Application.Tests.Cart;

public sealed class CartSnapshotServiceTests
{
    private static IReadOnlyList<CartItemDto> MakeItems() =>
        new List<CartItemDto>
        {
            new("item-1", new ProductDto("P1", "Produto", 99m, null, null, "cat", "mock"), 2, 198m)
        }.AsReadOnly();

    // ── PersistAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task PersistAsync_ComItens_ChamaSaveAsync()
    {
        var port = new Mock<ICartSnapshotPort>();
        var cart = new Mock<ICartPort>();
        var items = MakeItems();
        cart.Setup(c => c.GetItemsAsync("s1", default)).ReturnsAsync(items);

        var svc = new CartSnapshotService(port.Object);
        await svc.PersistAsync("s1", cart.Object);

        port.Verify(p => p.SaveAsync("s1", items, default), Times.Once);
        port.Verify(p => p.DeleteAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task PersistAsync_SemItens_ChamaDeleteAsync()
    {
        var port = new Mock<ICartSnapshotPort>();
        var cart = new Mock<ICartPort>();
        cart.Setup(c => c.GetItemsAsync("s1", default))
            .ReturnsAsync(new List<CartItemDto>().AsReadOnly());

        var svc = new CartSnapshotService(port.Object);
        await svc.PersistAsync("s1", cart.Object);

        port.Verify(p => p.DeleteAsync("s1", default), Times.Once);
        port.Verify(p => p.SaveAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<CartItemDto>>(), default), Times.Never);
    }

    [Fact]
    public async Task PersistAsync_ItemsNull_ChamaDeleteAsync()
    {
        var port = new Mock<ICartSnapshotPort>();
        var cart = new Mock<ICartPort>();
        cart.Setup(c => c.GetItemsAsync("s1", default))
            .ReturnsAsync((IReadOnlyList<CartItemDto>?)null!);

        var svc = new CartSnapshotService(port.Object);
        await svc.PersistAsync("s1", cart.Object);

        port.Verify(p => p.DeleteAsync("s1", default), Times.Once);
    }

    [Fact]
    public async Task PersistAsync_PortLancaExcecao_NaoPropaga()
    {
        var port = new Mock<ICartSnapshotPort>();
        var cart = new Mock<ICartPort>();
        cart.Setup(c => c.GetItemsAsync(It.IsAny<string>(), default)).ThrowsAsync(new Exception("BD offline"));

        var svc = new CartSnapshotService(port.Object);
        var act = async () => await svc.PersistAsync("s1", cart.Object);

        await act.Should().NotThrowAsync();
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ChamaPortDeleteAsync()
    {
        var port = new Mock<ICartSnapshotPort>();
        var svc  = new CartSnapshotService(port.Object);

        await svc.DeleteAsync("s1");

        port.Verify(p => p.DeleteAsync("s1", default), Times.Once);
    }

    // ── GetAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAsync_RetornaSnapshotDoPort()
    {
        var port     = new Mock<ICartSnapshotPort>();
        var expected = new CartSnapshotDto("s1", MakeItems(), 198m, DateTime.UtcNow);
        port.Setup(p => p.GetAsync("s1", default)).ReturnsAsync(expected);

        var svc    = new CartSnapshotService(port.Object);
        var result = await svc.GetAsync("s1");

        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetAsync_SemSnapshot_RetornaNull()
    {
        var port = new Mock<ICartSnapshotPort>();
        port.Setup(p => p.GetAsync("s1", default)).ReturnsAsync((CartSnapshotDto?)null);

        var svc    = new CartSnapshotService(port.Object);
        var result = await svc.GetAsync("s1");

        result.Should().BeNull();
    }
}
