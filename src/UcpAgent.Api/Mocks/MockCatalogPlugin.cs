using UcpAgent.SharedKernel.Attributes;
using UcpAgent.SharedKernel.Models;
using UcpAgent.SharedKernel.Ports;

namespace UcpAgent.Api.Mocks;

[CatalogPlugin("Mock")]
public sealed class MockCatalogPlugin : IProductCatalogPort
{
    public string SourceName => "Mock";

    private static readonly List<ProductDto> _produtos =
    [
        new("ml-001", "Smartphone Samsung Galaxy S24 128GB", 2_799.90m, "https://via.placeholder.com/300", "https://mercadolivre.com.br", "Celulares", "Mock", 3_499.90m, 50),
        new("ml-002", "Notebook Dell Inspiron 15 i5 16GB 512GB SSD", 3_499.00m, "https://via.placeholder.com/300", "https://mercadolivre.com.br", "Informática", "Mock", 4_199.00m, 12),
        new("ml-003", "Smart TV LG 55\" 4K OLED", 4_199.90m, "https://via.placeholder.com/300", "https://mercadolivre.com.br", "TV e Vídeo", "Mock", 5_999.90m, 8),
        new("ml-004", "Fone de Ouvido Sony WH-1000XM5 Bluetooth", 1_299.00m, "https://via.placeholder.com/300", "https://mercadolivre.com.br", "Áudio", "Mock", 1_799.00m, 35),
        new("ml-005", "Geladeira Brastemp Frost Free 375L Inox", 3_199.00m, "https://via.placeholder.com/300", "https://mercadolivre.com.br", "Eletrodomésticos", "Mock", null, 5),
        new("ml-006", "Cafeteira Nespresso Vertuo Pop", 599.90m, "https://via.placeholder.com/300", "https://mercadolivre.com.br", "Eletrodomésticos", "Mock", 799.90m, 20),
        new("ml-007", "Tênis Nike Air Max 270 Masculino", 499.99m, "https://via.placeholder.com/300", "https://mercadolivre.com.br", "Esportes", "Mock", 699.99m, 30),
        new("ml-008", "Livro: Clean Architecture - Robert Martin", 89.90m, "https://via.placeholder.com/300", "https://mercadolivre.com.br", "Livros", "Mock", null, 100),
        new("ml-009", "Cadeira Gamer DXRacer OH/D61 Preta/Vermelha", 1_599.00m, "https://via.placeholder.com/300", "https://mercadolivre.com.br", "Móveis", "Mock", 2_199.00m, 7),
        new("ml-010", "Mouse Logitech MX Master 3S Sem Fio", 449.90m, "https://via.placeholder.com/300", "https://mercadolivre.com.br", "Informática", "Mock", 599.90m, 40),
    ];

    public Task<SearchResult> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        var query = request.Query.ToLower();

        var filtrados = _produtos
            .Where(p =>
                p.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (p.Category?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .Where(p => request.MinPrice == null || p.Price >= request.MinPrice)
            .Where(p => request.MaxPrice == null || p.Price <= request.MaxPrice)
            .ToList();

        var paginados = filtrados
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Task.FromResult(new SearchResult(paginados, filtrados.Count, request.Page, request.PageSize, SourceName));
    }
}
