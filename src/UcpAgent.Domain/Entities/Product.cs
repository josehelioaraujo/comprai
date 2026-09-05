namespace UcpAgent.Domain.Entities;

public class Product
{
    public string Id { get; private set; }
    public string Title { get; private set; }
    public decimal Price { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? Url { get; private set; }
    public string? Category { get; private set; }
    public string Source { get; private set; }
    public decimal? OriginalPrice { get; private set; }
    public int? AvailableQuantity { get; private set; }

    private Product() { Id = ""; Title = ""; Source = ""; }

    public static Product Create(
        string id, string title, decimal price, string source,
        string? imageUrl = null, string? url = null, string? category = null,
        decimal? originalPrice = null, int? availableQuantity = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        if (price < 0) throw new ArgumentException("Preço não pode ser negativo.", nameof(price));

        return new Product
        {
            Id = id,
            Title = title,
            Price = price,
            Source = source,
            ImageUrl = imageUrl,
            Url = url,
            Category = category,
            OriginalPrice = originalPrice,
            AvailableQuantity = availableQuantity
        };
    }

    public bool HasDiscount => OriginalPrice.HasValue && OriginalPrice.Value > Price;
    public decimal DiscountPercentage => HasDiscount ? Math.Round((1 - Price / OriginalPrice!.Value) * 100, 1) : 0;
}
