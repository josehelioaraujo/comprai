using UcpAgent.Domain.Entities;
using Xunit;

namespace UcpAgent.Domain.Tests;

public class CartItemTests
{
    private static Product MakeProduct() =>
        Product.Create("p1", "Produto", 50m, "ml");

    [Fact]
    public void Create_ValidArgs_ReturnsCartItem()
    {
        var item = CartItem.Create(MakeProduct(), 3);
        Assert.NotEmpty(item.ItemId);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(150m, item.Subtotal);
    }

    [Fact]
    public void Create_ZeroQuantity_Throws()
    {
        Assert.Throws<ArgumentException>(() => CartItem.Create(MakeProduct(), 0));
    }

    [Fact]
    public void Create_NegativeQuantity_Throws()
    {
        Assert.Throws<ArgumentException>(() => CartItem.Create(MakeProduct(), -1));
    }

    [Fact]
    public void Create_NullProduct_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CartItem.Create(null!, 1));
    }

    [Fact]
    public void UpdateQuantity_ValidQty_UpdatesQuantity()
    {
        var item = CartItem.Create(MakeProduct(), 1);
        item.UpdateQuantity(5);
        Assert.Equal(5, item.Quantity);
        Assert.Equal(250m, item.Subtotal);
    }

    [Fact]
    public void UpdateQuantity_Zero_Throws()
    {
        var item = CartItem.Create(MakeProduct(), 1);
        Assert.Throws<ArgumentException>(() => item.UpdateQuantity(0));
    }
}
