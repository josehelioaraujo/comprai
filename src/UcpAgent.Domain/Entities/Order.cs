using UcpAgent.Domain.Enums;

namespace UcpAgent.Domain.Entities;

public class Order
{
    public string Id { get; private set; }
    public string SessionId { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public decimal Total => _items.Sum(i => i.Subtotal);
    public OrderStatus Status { get; private set; }
    public string CustomerName { get; private set; }
    public string CustomerEmail { get; private set; }
    public string? TrackingCode { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<OrderItem> _items = [];

    private Order() { Id = ""; SessionId = ""; CustomerName = ""; CustomerEmail = ""; }

    public static Order FromCart(Cart cart, string customerName, string customerEmail)
    {
        if (cart.IsEmpty) throw new InvalidOperationException("Não é possível criar pedido com carrinho vazio.");
        ArgumentException.ThrowIfNullOrWhiteSpace(customerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerEmail);

        var order = new Order
        {
            Id = Guid.NewGuid().ToString("N"),
            SessionId = cart.SessionId,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        order._items.AddRange(cart.Items.Select(OrderItem.FromCartItem));
        return order;
    }

    public void UpdateStatus(OrderStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTracking(string trackingCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingCode);
        TrackingCode = trackingCode;
        UpdatedAt = DateTime.UtcNow;
    }
}
