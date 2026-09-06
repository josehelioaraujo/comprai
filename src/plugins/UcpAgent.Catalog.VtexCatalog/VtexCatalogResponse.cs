namespace UcpAgent.Catalog.VtexCatalog;

internal sealed record VtexProduct(
    string ProductId,
    string ProductName,
    string Link,
    List<string> Categories,
    List<VtexItem> Items);

internal sealed record VtexItem(
    List<VtexImage> Images,
    List<VtexSeller> Sellers);

internal sealed record VtexImage(string ImageUrl);

internal sealed record VtexSeller(VtexOffer CommertialOffer);

internal sealed record VtexOffer(decimal Price, decimal ListPrice, int AvailableQuantity);
