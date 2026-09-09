using UcpAgent.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace UcpAgent.Domain.Tests;

public class CartRemoveEdgeCaseTests
{
    private static Product MakeProduct(string id = "p1") =>
        Product.Create(id, $"Produto {id}", 99m, "ml");

    [Fact]
    public void RemoveItem_ItemInexistente_LancaInvalidOperationException()
    {
        var cart = Cart.Create("session-1");
        cart.AddItem(MakeProduct("p1"), 1);

        Action act = () => cart.RemoveItem("id-que-nao-existe");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddItem_ProdutoDiferentesMesmaFonte_CriaItensSeperados()
    {
        var cart = Cart.Create("session-1");
        cart.AddItem(MakeProduct("p1"), 1);
        cart.AddItem(MakeProduct("p2"), 1);

        cart.Items.Should().HaveCount(2);
    }

    [Fact]
    public void AddItem_MesmoProdutoMesmaFonte_AcumulaQuantidade()
    {
        var cart    = Cart.Create("session-1");
        var product = MakeProduct("p1");

        cart.AddItem(product, 2);
        cart.AddItem(product, 3);

        cart.Items.Should().HaveCount(1);
        cart.Items[0].Quantity.Should().Be(5);
    }

    [Fact]
    public void Cart_UpdatedAt_AlteradoAposAddItem()
    {
        var cart   = Cart.Create("session-1");
        var before = cart.UpdatedAt;

        System.Threading.Thread.Sleep(1); // garante diferença de timestamp
        cart.AddItem(MakeProduct(), 1);

        cart.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Cart_UpdatedAt_AlteradoAposRemoveItem()
    {
        var cart = Cart.Create("session-1");
        var item = cart.AddItem(MakeProduct(), 1);
        var before = cart.UpdatedAt;

        System.Threading.Thread.Sleep(1);
        cart.RemoveItem(item.ItemId);

        cart.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Cart_IsEmpty_TrueAposClear()
    {
        var cart = Cart.Create("session-1");
        cart.AddItem(MakeProduct("p1"), 1);
        cart.AddItem(MakeProduct("p2"), 2);

        cart.Clear();

        cart.IsEmpty.Should().BeTrue();
        cart.Total.Should().Be(0m);
        cart.ItemCount.Should().Be(0);
    }
}
