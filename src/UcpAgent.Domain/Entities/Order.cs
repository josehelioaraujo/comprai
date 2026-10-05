using UcpAgent.Domain.Enums;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Domain.Entities;

public class Order
{
    public string       Id            { get; set; } = Guid.NewGuid().ToString("N");
    public string       SessionId     { get; set; } = string.Empty;
    public Guid?        CustomerId    { get; set; }
    public OrderStatus  Status        { get; set; }
    public string       CustomerName  { get; private set; } = string.Empty;
    public string       CustomerEmail { get; private set; } = string.Empty;
    public string?      TrackingCode  { get; private set; }
    public DateTime     CreatedAt     { get; set; } = DateTime.UtcNow;
    public DateTime     UpdatedAt     { get; set; } = DateTime.UtcNow;

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public decimal Total => _items.Sum(i => i.Subtotal);

    private readonly List<OrderItem> _items = [];

    /// <summary>
    /// Adiciona item a partir de CartItemDto (fluxo F1 — Redis→PG).
    /// CartItemDto: record(ItemId, Product, Quantity, Subtotal)
    /// </summary>
    public void AddItemFromCart(CartItemDto item)
        => _items.Add(OrderItem.FromPrimitives(
            item.Product.Id,
            item.Product.Title,
            item.Product.Source,
            item.Product.Price,
            item.Quantity));

    public static Order FromCart(Cart cart, string customerName, string customerEmail)
    {
        if (cart.IsEmpty)
            throw new InvalidOperationException("Não é possível criar pedido com carrinho vazio.");
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerEmail);

        var order = new Order
        {
            Id            = Guid.NewGuid().ToString("N"),
            SessionId     = cart.SessionId,
            CustomerName  = customerName,
            CustomerEmail = customerEmail,
            Status        = OrderStatus.Pending,
            CreatedAt     = DateTime.UtcNow,
            UpdatedAt     = DateTime.UtcNow
        };
        order._items.AddRange(cart.Items.Select(OrderItem.FromCartItem));
        return order;
    }

    public void UpdateStatus(OrderStatus newStatus)
    {
        Status    = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTracking(string trackingCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingCode);
        TrackingCode = trackingCode;
        UpdatedAt    = DateTime.UtcNow;
    }
}
