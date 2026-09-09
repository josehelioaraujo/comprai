using UcpAgent.Domain.Entities;
using Xunit;

namespace UcpAgent.Domain.Tests;

public class ProductTests
{
    [Fact]
    public void Create_ValidArgs_ReturnsProduct()
    {
        var p = Product.Create("1", "Notebook", 2500m, "ml");
        Assert.Equal("1", p.Id);
        Assert.Equal("Notebook", p.Title);
        Assert.Equal(2500m, p.Price);
        Assert.Equal("ml", p.Source);
    }

    [Theory]
    [InlineData("", "Título", 100, "ml")]
    [InlineData("1", "", 100, "ml")]
    [InlineData("1", "Título", 100, "")]
    public void Create_InvalidArgs_Throws(string id, string title, decimal price, string source)
    {
        Assert.ThrowsAny<ArgumentException>(() => Product.Create(id, title, price, source));
    }

    [Fact]
    public void Create_NegativePrice_Throws()
    {
        Assert.Throws<ArgumentException>(() => Product.Create("1", "Produto", -1m, "ml"));
    }

    [Fact]
    public void HasDiscount_WithOriginalPrice_ReturnsTrue()
    {
        var p = Product.Create("1", "Notebook", 1800m, "ml", originalPrice: 2000m);
        Assert.True(p.HasDiscount);
        Assert.Equal(10m, p.DiscountPercentage);
    }

    [Fact]
    public void HasDiscount_WithoutOriginalPrice_ReturnsFalse()
    {
        var p = Product.Create("1", "Notebook", 1800m, "ml");
        Assert.False(p.HasDiscount);
        Assert.Equal(0m, p.DiscountPercentage);
    }

    [Fact]
    public void Create_WithAllOptionalFields_ReturnsProduct()
    {
        var p = Product.Create("1", "Notebook", 2500m, "ml",
            imageUrl: "http://img.com", url: "http://ml.com",
            category: "eletronicos", originalPrice: 3000m, availableQuantity: 5);
        Assert.Equal("http://img.com", p.ImageUrl);
        Assert.Equal("eletronicos", p.Category);
        Assert.Equal(5, p.AvailableQuantity);
    }
}
