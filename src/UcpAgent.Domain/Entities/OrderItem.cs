namespace UcpAgent.Domain.Entities;

public class OrderItem
{
    public string  ProductId    { get; private set; }
    public string  ProductTitle { get; private set; }
    public string  Source       { get; private set; }
    public decimal UnitPrice    { get; private set; }
    public int     Quantity     { get; private set; }
    public decimal Subtotal     => UnitPrice * Quantity;

    private OrderItem() { ProductId = ""; ProductTitle = ""; Source = ""; }

    public static OrderItem FromCartItem(CartItem cartItem) =>
        new()
        {
            ProductId    = cartItem.Product.Id,
            ProductTitle = cartItem.Product.Title,
            Source       = cartItem.Product.Source,
            UnitPrice    = cartItem.Product.Price,
            Quantity     = cartItem.Quantity
        };

    /// <summary>
    /// Factory a partir de primitivos — usada no fluxo F1 (CartItemDto → PG).
    /// </summary>
    public static OrderItem FromPrimitives(
        string productId, string productTitle, string source,
        decimal unitPrice, int quantity) =>
        new()
        {
            ProductId    = productId,
            ProductTitle = productTitle,
            Source       = source,
            UnitPrice    = unitPrice,
            Quantity     = quantity
        };
}
