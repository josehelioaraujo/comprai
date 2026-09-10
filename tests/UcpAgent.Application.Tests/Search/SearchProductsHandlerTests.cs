using Moq;
using UcpAgent.Application.Search;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;
using Xunit;

namespace UcpAgent.Application.Tests.Search;

public sealed class SearchProductsHandlerTests
{
    private static ProductDto MakeProduct(string id, string source, decimal price, int qty = 5) =>
        new(id, $"Produto {id}", price, null, null, null, source, null, qty);

    private static SearchResult MakeResult(IEnumerable<ProductDto> items, string source) =>
        new(items.ToList(), items.Count(), 1, 20, source);

    [Fact]
    public async Task Handle_SingleCatalog_ReturnsProducts()
    {
        // Arrange
        var catalog = new Mock<IProductCatalogPort>();
        var product = MakeProduct("1", "mock", 100m);

        catalog.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
               .ReturnsAsync(MakeResult([product], "mock"));
        catalog.Setup(c => c.SourceName).Returns("mock");

        var handler = new SearchProductsHandler([catalog.Object]);
        var query   = new SearchProductsQuery("notebook");

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("Produto 1", result.Value.Items[0].Title);
    }

    [Fact]
    public async Task Handle_MultipleCatalogs_AggregatesResults()
    {
        // Arrange
        var catalog1 = new Mock<IProductCatalogPort>();
        var catalog2 = new Mock<IProductCatalogPort>();

        catalog1.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
                .ReturnsAsync(MakeResult([MakeProduct("A", "ml", 200m)], "ml"));
        catalog1.Setup(c => c.SourceName).Returns("ml");

        catalog2.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
                .ReturnsAsync(MakeResult([MakeProduct("B", "shopify", 150m)], "shopify"));
        catalog2.Setup(c => c.SourceName).Returns("shopify");

        var handler = new SearchProductsHandler([catalog1.Object, catalog2.Object]);

        // Act
        var result = await handler.Handle(new SearchProductsQuery("tênis"), default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
    }

    [Fact]
    public async Task Handle_DuplicateProducts_DeduplicatesBySourceAndId()
    {
        // Arrange
        var catalog1 = new Mock<IProductCatalogPort>();
        var catalog2 = new Mock<IProductCatalogPort>();
        var duplicate = MakeProduct("X", "ml", 100m);

        catalog1.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
                .ReturnsAsync(MakeResult([duplicate], "ml"));
        catalog1.Setup(c => c.SourceName).Returns("ml");

        catalog2.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
                .ReturnsAsync(MakeResult([duplicate], "ml")); // mesmo produto
        catalog2.Setup(c => c.SourceName).Returns("ml");

        var handler = new SearchProductsHandler([catalog1.Object, catalog2.Object]);

        // Act
        var result = await handler.Handle(new SearchProductsQuery("tênis"), default);

        // Assert
        Assert.Single(result.Value!.Items); // deduplicado
    }

    [Fact]
    public async Task Handle_CatalogFails_ReturnsPartialResults()
    {
        // Arrange
        var okCatalog   = new Mock<IProductCatalogPort>();
        var failCatalog = new Mock<IProductCatalogPort>();

        okCatalog.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
                 .ReturnsAsync(MakeResult([MakeProduct("1", "ok", 50m)], "ok"));
        okCatalog.Setup(c => c.SourceName).Returns("ok");

        failCatalog.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
                   .ThrowsAsync(new HttpRequestException("timeout"));
        failCatalog.Setup(c => c.SourceName).Returns("fail");

        var handler = new SearchProductsHandler([okCatalog.Object, failCatalog.Object]);

        // Act
        var result = await handler.Handle(new SearchProductsQuery("produto"), default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items); // só os do catálogo ok
    }

    [Fact]
    public async Task Handle_OrdersByAvailabilityThenPrice()
    {
        // Arrange
        var catalog = new Mock<IProductCatalogPort>();

        var products = new[]
        {
            MakeProduct("caro",       "ml", 500m, qty: 5),
            MakeProduct("sem-estoque","ml", 10m,  qty: 0),
            MakeProduct("barato",     "ml", 50m,  qty: 5),
        };

        catalog.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
               .ReturnsAsync(MakeResult(products, "ml"));
        catalog.Setup(c => c.SourceName).Returns("ml");

        var handler = new SearchProductsHandler([catalog.Object]);

        // Act
        var result = await handler.Handle(new SearchProductsQuery("produto"), default);
        var items  = result.Value!.Items;

        // Assert — barato antes de caro, sem estoque por último
        Assert.Equal("barato",      items[0].Id);
        Assert.Equal("caro",        items[1].Id);
        Assert.Equal("sem-estoque", items[2].Id);
    }
}

