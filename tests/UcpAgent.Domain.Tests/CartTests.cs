using UcpAgent.Domain.Entities;
using Xunit;

namespace UcpAgent.Domain.Tests;

public class CartTests
{
    private static Product MakeProduct(string id = "p1", decimal price = 99.90m) =>
        Product.Create(id, "Produto Teste", price, "Teste");

    [Fact]
    public void AddItem_DeveAdicionarItemAoCarrinho()
    {
        var cart = Cart.Create("session-1");
        var product = MakeProduct();

        cart.AddItem(product, 2);

        Assert.Single(cart.Items);
        Assert.Equal(2, cart.ItemCount);
        Assert.Equal(199.80m, cart.Total);
    }

    [Fact]
    public void AddItem_MesmoProduto_DeveAcumularQuantidade()
    {
        var cart = Cart.Create("session-1");
        var product = MakeProduct();

        cart.AddItem(product, 1);
        cart.AddItem(product, 2);

        Assert.Single(cart.Items);
        Assert.Equal(3, cart.Items[0].Quantity);
    }

    [Fact]
    public void RemoveItem_DeveRemoverItemDoCarrinho()
    {
        var cart = Cart.Create("session-1");
        var item = cart.AddItem(MakeProduct(), 1);

        cart.RemoveItem(item.ItemId);

        Assert.Empty(cart.Items);
        Assert.True(cart.IsEmpty);
    }

    [Fact]
    public void Clear_DeveEsvaziarCarrinho()
    {
        var cart = Cart.Create("session-1");
        cart.AddItem(MakeProduct("p1"), 1);
        cart.AddItem(MakeProduct("p2"), 2);

        cart.Clear();

        Assert.True(cart.IsEmpty);
        Assert.Equal(0m, cart.Total);
    }

    [Fact]
    public void CreateCart_ComSessionIdVazio_DeveLancarExcecao()
    {
        Assert.Throws<ArgumentException>(() => Cart.Create(""));
    }
}
