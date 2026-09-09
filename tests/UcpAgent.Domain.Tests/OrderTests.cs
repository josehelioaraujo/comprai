using UcpAgent.Domain.Entities;
using UcpAgent.Domain.Enums;
using Xunit;

namespace UcpAgent.Domain.Tests;

public class OrderTests
{
    private static Product MakeProduct(string id = "p1", decimal price = 100m) =>
        Product.Create(id, $"Produto {id}", price, "ml");

    private static Cart MakeCartWithItem()
    {
        var cart = Cart.Create("session-1");
        cart.AddItem(MakeProduct(), 2);
        return cart;
    }

    [Fact]
    public void FromCart_ValidCart_CreatesOrder()
    {
        var cart = MakeCartWithItem();
        var order = Order.FromCart(cart, "Helio", "helio@test.com");

        Assert.NotEmpty(order.Id);
        Assert.Equal("session-1", order.SessionId);
        Assert.Equal("Helio", order.CustomerName);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Single(order.Items);
        Assert.Equal(200m, order.Total);
    }

    [Fact]
    public void FromCart_EmptyCart_Throws()
    {
        var cart = Cart.Create("session-empty");
        Assert.Throws<InvalidOperationException>(() =>
            Order.FromCart(cart, "Helio", "helio@test.com"));
    }

    [Theory]
    [InlineData("", "helio@test.com")]
    [InlineData("Helio", "")]
    public void FromCart_InvalidCustomer_Throws(string name, string email)
    {
        var cart = MakeCartWithItem();
        Assert.ThrowsAny<ArgumentException>(() => Order.FromCart(cart, name, email));
    }

    [Fact]
    public void UpdateStatus_ChangesStatus()
    {
        var order = Order.FromCart(MakeCartWithItem(), "Helio", "helio@test.com");
        order.UpdateStatus(OrderStatus.Confirmed);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void SetTracking_ValidCode_SetsTracking()
    {
        var order = Order.FromCart(MakeCartWithItem(), "Helio", "helio@test.com");
        order.SetTracking("BR123456789");
        Assert.Equal("BR123456789", order.TrackingCode);
    }

    [Fact]
    public void SetTracking_EmptyCode_Throws()
    {
        var order = Order.FromCart(MakeCartWithItem(), "Helio", "helio@test.com");
        Assert.ThrowsAny<ArgumentException>(() => order.SetTracking(""));
    }

    [Fact]
    public void OrderItem_Subtotal_IsCorrect()
    {
        var cart = Cart.Create("s1");
        cart.AddItem(MakeProduct("p1", 50m), 3);
        var order = Order.FromCart(cart, "Helio", "helio@test.com");
        Assert.Equal(150m, order.Items[0].Subtotal);
    }
}
