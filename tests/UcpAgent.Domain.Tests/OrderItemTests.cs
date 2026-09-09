using UcpAgent.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace UcpAgent.Domain.Tests;

public class OrderItemTests
{
    private static Product MakeProduct(string id = "p1", decimal price = 100m, string source = "ml") =>
        Product.Create(id, $"Produto {id}", price, source);

    private static CartItem MakeCartItem(decimal price = 100m, int qty = 2) =>
        CartItem.Create(MakeProduct(price: price), qty);

    [Fact]
    public void FromCartItem_CopiaPropriedadesCorretamente()
    {
        var cartItem  = CartItem.Create(MakeProduct("p1", 150m, "shopify"), 3);
        var orderItem = OrderItem.FromCartItem(cartItem);

        orderItem.ProductId.Should().Be("p1");
        orderItem.ProductTitle.Should().Be("Produto p1");
        orderItem.Source.Should().Be("shopify");
        orderItem.UnitPrice.Should().Be(150m);
        orderItem.Quantity.Should().Be(3);
    }

    [Fact]
    public void FromCartItem_SubtotalCalculadoCorretamente()
    {
        var cartItem  = MakeCartItem(price: 75m, qty: 4);
        var orderItem = OrderItem.FromCartItem(cartItem);

        orderItem.Subtotal.Should().Be(300m);
    }

    [Fact]
    public void FromCartItem_QuantidadeUm_SubtotalIgualAoPreco()
    {
        var cartItem  = MakeCartItem(price: 250m, qty: 1);
        var orderItem = OrderItem.FromCartItem(cartItem);

        orderItem.Subtotal.Should().Be(orderItem.UnitPrice);
    }

    [Fact]
    public void FromCartItem_FontePreservada()
    {
        var product  = Product.Create("x1", "Tênis", 200m, "vtex");
        var cartItem = CartItem.Create(product, 1);
        var item     = OrderItem.FromCartItem(cartItem);

        item.Source.Should().Be("vtex");
    }
}
