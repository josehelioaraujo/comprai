namespace UcpAgent.Catalog.MercadoLivre;

internal sealed record MlSearchResponse(
    MlPaging Paging,
    List<MlItem> Results);

internal sealed record MlPaging(
    int Total,
    int Offset,
    int Limit);

internal sealed record MlItem(
    string Id,
    string Title,
    decimal Price,
    decimal? Original_Price,
    string Thumbnail,
    string Permalink,
    string Category_Id,
    int Available_Quantity,
    string Currency_Id);
