using UcpAgent.Domain.Entities;
using Xunit;
using FluentAssertions;

namespace UcpAgent.Domain.Tests;

public class CartItemTests
{
    private static Product MakeProduct(decimal price = 50m) =>
        Product.Create("p1", "Produto Teste", price, "ml");

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidArgs_CriaItemComSubtotalCorreto()
    {
        var item = CartItem.Create(MakeProduct(100m), 3);

        item.Quantity.Should().Be(3);
        item.Subtotal.Should().Be(300m);
        item.ItemId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Create_QuantidadeZero_LancaExcecao()
    {
        Action act = () => CartItem.Create(MakeProduct(), 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_QuantidadeNegativa_LancaExcecao()
    {
        Action act = () => CartItem.Create(MakeProduct(), -1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ProdutoNulo_LancaExcecao()
    {
        Action act = () => CartItem.Create(null!, 1);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_GeraItemIdUnico()
    {
        var item1 = CartItem.Create(MakeProduct(), 1);
        var item2 = CartItem.Create(MakeProduct(), 1);

        item1.ItemId.Should().NotBe(item2.ItemId);
    }

    // ── UpdateQuantity ────────────────────────────────────────────────────────

    [Fact]
    public void UpdateQuantity_ValorValido_AtualizaQuantidadeESubtotal()
    {
        var item = CartItem.Create(MakeProduct(50m), 1);

        item.UpdateQuantity(4);

        item.Quantity.Should().Be(4);
        item.Subtotal.Should().Be(200m);
    }

    [Fact]
    public void UpdateQuantity_Zero_LancaExcecao()
    {
        var item = CartItem.Create(MakeProduct(), 1);

        Action act = () => item.UpdateQuantity(0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateQuantity_Negativo_LancaExcecao()
    {
        var item = CartItem.Create(MakeProduct(), 1);

        Action act = () => item.UpdateQuantity(-5);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateQuantity_NaoAlteraItemId()
    {
        var item = CartItem.Create(MakeProduct(), 1);
        var idAntes = item.ItemId;

        item.UpdateQuantity(3);

        item.ItemId.Should().Be(idAntes);
    }
}
