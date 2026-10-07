using UcpAgent.Domain.Enums;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Domain.Entities;

public class Order
{
    public string       Id                 { get; set; } = Guid.NewGuid().ToString();
    public string       SessionId          { get; set; } = string.Empty;
    public Guid?        CustomerId         { get; set; }
    public OrderStatus  Status             { get; set; }
    public string       CustomerName       { get; private set; } = string.Empty;
    public string       CustomerEmail      { get; private set; } = string.Empty;
    public string?      TrackingCode       { get; private set; }
    // Endereço snapshot
    public string?      ShippingZip        { get; set; }
    public string?      ShippingStreet     { get; set; }
    public string?      ShippingNumber     { get; set; }
    public string?      ShippingComplement { get; set; }
    public string?      ShippingCity       { get; set; }
    public string?      ShippingState      { get; set; }
    public DateTime     CreatedAt          { get; set; } = DateTime.UtcNow;
    public DateTime     UpdatedAt          { get; set; } = DateTime.UtcNow;

    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public decimal Total => _items.Sum(i => i.Subtotal);

    private readonly List<OrderItem> _items = [];

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
            Id            = Guid.NewGuid().ToString(),
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
