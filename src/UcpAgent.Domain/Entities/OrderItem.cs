namespace UcpAgent.Domain.Entities;

public class OrderItem
{
    public string ProductId { get; private set; }
    public string ProductTitle { get; private set; }
    public string Source { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal Subtotal => UnitPrice * Quantity;

    private OrderItem() { ProductId = ""; ProductTitle = ""; Source = ""; }

    public static OrderItem FromCartItem(CartItem cartItem) =>
        new()
        {
            ProductId = cartItem.Product.Id,
            ProductTitle = cartItem.Product.Title,
            Source = cartItem.Product.Source,
            UnitPrice = cartItem.Product.Price,
            Quantity = cartItem.Quantity
        };
}
