namespace UcpAgent.Catalog.VtexSearch;

internal sealed record VtexSearchResponse(
    List<VtexSearchProduct> Products,
    VtexSearchPagination Pagination);

internal sealed record VtexSearchPagination(int Total, int From, int To);

internal sealed record VtexSearchProduct(
    string ProductId,
    string ProductName,
    string Link,
    List<string> Categories,
    List<VtexSearchSku> Items);

internal sealed record VtexSearchSku(
    List<VtexSearchImage> Images,
    List<VtexSearchSeller> Sellers);

internal sealed record VtexSearchImage(string ImageUrl);

internal sealed record VtexSearchSeller(VtexSearchOffer CommertialOffer);

internal sealed record VtexSearchOffer(decimal Price, decimal ListPrice, int AvailableQuantity);
