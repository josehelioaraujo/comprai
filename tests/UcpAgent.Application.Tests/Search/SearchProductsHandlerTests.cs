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
        var catalog = new Mock<IProductCatalogPort>();
        var product = MakeProduct("1", "mock", 100m);
        catalog.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
               .ReturnsAsync(MakeResult([product], "mock"));
        catalog.Setup(c => c.SourceName).Returns("mock");
        var handler = new SearchProductsHandler([catalog.Object]);

        var result = await handler.Handle(new SearchProductsQuery("notebook"), default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("Produto 1", result.Value.Items[0].Title);
    }

    [Fact]
    public async Task Handle_MultipleCatalogs_AggregatesResults()
    {
        var catalog1 = new Mock<IProductCatalogPort>();
        var catalog2 = new Mock<IProductCatalogPort>();
        catalog1.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
                .ReturnsAsync(MakeResult([MakeProduct("A", "ml", 200m)], "ml"));
        catalog1.Setup(c => c.SourceName).Returns("ml");
        catalog2.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
                .ReturnsAsync(MakeResult([MakeProduct("B", "shopify", 150m)], "shopify"));
        catalog2.Setup(c => c.SourceName).Returns("shopify");
        var handler = new SearchProductsHandler([catalog1.Object, catalog2.Object]);

        var result = await handler.Handle(new SearchProductsQuery("tênis"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
    }

    [Fact]
    public async Task Handle_MultipleCatalogs_AggregatesTotalItems()
    {
        var catalog1 = new Mock<IProductCatalogPort>();
        var catalog2 = new Mock<IProductCatalogPort>();
        // catalog1 tem 2 items, catalog2 tem 3 items
        var items1 = Enumerable.Range(1, 2).Select(i => MakeProduct($"A{i}", "ml", i * 10m)).ToList();
        var items2 = Enumerable.Range(1, 3).Select(i => MakeProduct($"B{i}", "shopify", i * 20m)).ToList();
        catalog1.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
                .ReturnsAsync(new SearchResult(items1, 10, 1, 20, "ml")); // total=10
        catalog1.Setup(c => c.SourceName).Returns("ml");
        catalog2.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
                .ReturnsAsync(new SearchResult(items2, 15, 1, 20, "shopify")); // total=15
        catalog2.Setup(c => c.SourceName).Returns("shopify");
        var handler = new SearchProductsHandler([catalog1.Object, catalog2.Object]);

        var result = await handler.Handle(new SearchProductsQuery("produto"), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(25, result.Value!.TotalItems); // 10 + 15
        Assert.Equal("aggregated", result.Value.Source);
    }

    [Fact]
    public async Task Handle_DuplicateProducts_DeduplicatesBySourceAndId()
    {
        var catalog1 = new Mock<IProductCatalogPort>();
        var catalog2 = new Mock<IProductCatalogPort>();
        var duplicate = MakeProduct("X", "ml", 100m);
        catalog1.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
                .ReturnsAsync(MakeResult([duplicate], "ml"));
        catalog1.Setup(c => c.SourceName).Returns("ml");
        catalog2.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
                .ReturnsAsync(MakeResult([duplicate], "ml"));
        catalog2.Setup(c => c.SourceName).Returns("ml");
        var handler = new SearchProductsHandler([catalog1.Object, catalog2.Object]);

        var result = await handler.Handle(new SearchProductsQuery("tênis"), default);

        Assert.Single(result.Value!.Items);
    }

    [Fact]
    public async Task Handle_CatalogFails_ReturnsPartialResults()
    {
        var okCatalog   = new Mock<IProductCatalogPort>();
        var failCatalog = new Mock<IProductCatalogPort>();
        okCatalog.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
                 .ReturnsAsync(MakeResult([MakeProduct("1", "ok", 50m)], "ok"));
        okCatalog.Setup(c => c.SourceName).Returns("ok");
        failCatalog.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
                   .ThrowsAsync(new HttpRequestException("timeout"));
        failCatalog.Setup(c => c.SourceName).Returns("fail");
        var handler = new SearchProductsHandler([okCatalog.Object, failCatalog.Object]);

        var result = await handler.Handle(new SearchProductsQuery("produto"), default);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
    }

    [Fact]
    public async Task Handle_OrdersByAvailabilityThenPrice()
    {
        var catalog = new Mock<IProductCatalogPort>();
        var products = new[]
        {
            MakeProduct("caro",        "ml", 500m, qty: 5),
            MakeProduct("sem-estoque", "ml", 10m,  qty: 0),
            MakeProduct("barato",      "ml", 50m,  qty: 5),
        };
        catalog.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
               .ReturnsAsync(MakeResult(products, "ml"));
        catalog.Setup(c => c.SourceName).Returns("ml");
        var handler = new SearchProductsHandler([catalog.Object]);

        var result = await handler.Handle(new SearchProductsQuery("produto"), default);
        var items  = result.Value!.Items;

        Assert.Equal("barato",      items[0].Id);
        Assert.Equal("caro",        items[1].Id);
        Assert.Equal("sem-estoque", items[2].Id);
    }

    [Fact]
    public async Task Handle_NoCatalogs_ReturnsEmptySuccess()
    {
        var handler = new SearchProductsHandler([]);

        var result = await handler.Handle(new SearchProductsQuery("produto"), default);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalItems);
    }

    [Fact]
    public async Task Handle_ResultSourceIsAggregated()
    {
        var catalog = new Mock<IProductCatalogPort>();
        catalog.Setup(c => c.SearchAsync(It.IsAny<SearchRequest>(), default))
               .ReturnsAsync(MakeResult([MakeProduct("1", "ml", 10m)], "ml"));
        catalog.Setup(c => c.SourceName).Returns("ml");
        var handler = new SearchProductsHandler([catalog.Object]);

        var result = await handler.Handle(new SearchProductsQuery("x"), default);

        Assert.Equal("aggregated", result.Value!.Source);
    }
}
