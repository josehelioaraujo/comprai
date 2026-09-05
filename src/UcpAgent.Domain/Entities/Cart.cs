namespace UcpAgent.Domain.Entities;

public class Cart
{
    public string SessionId { get; private set; }
    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();
    public decimal Total => _items.Sum(i => i.Subtotal);
    public int ItemCount => _items.Sum(i => i.Quantity);
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<CartItem> _items = [];

    private Cart() { SessionId = ""; }

    public static Cart Create(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return new Cart { SessionId = sessionId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
    }

    public CartItem AddItem(Product product, int quantity)
    {
        var existing = _items.FirstOrDefault(i => i.Product.Id == product.Id && i.Product.Source == product.Source);
        if (existing is not null)
        {
            existing.UpdateQuantity(existing.Quantity + quantity);
            UpdatedAt = DateTime.UtcNow;
            return existing;
        }

        var item = CartItem.Create(product, quantity);
        _items.Add(item);
        UpdatedAt = DateTime.UtcNow;
        return item;
    }

    public void RemoveItem(string itemId)
    {
        var item = _items.FirstOrDefault(i => i.ItemId == itemId)
            ?? throw new InvalidOperationException($"Item '{itemId}' não encontrado no carrinho.");
        _items.Remove(item);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Clear()
    {
        _items.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsEmpty => _items.Count == 0;
}
