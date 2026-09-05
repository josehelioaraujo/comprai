namespace UcpAgent.Domain.Entities;

public class CartItem
{
    public string ItemId { get; private set; }
    public Product Product { get; private set; }
    public int Quantity { get; private set; }
    public decimal Subtotal => Product.Price * Quantity;

    private CartItem() { ItemId = ""; Product = null!; }

    public static CartItem Create(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (quantity <= 0) throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantity));

        return new CartItem
        {
            ItemId = Guid.NewGuid().ToString("N"),
            Product = product,
            Quantity = quantity
        };
    }

    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantity));
        Quantity = quantity;
    }
}
