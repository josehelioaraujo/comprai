using Bogus;
using UcpAgent.SharedKernel.Models;

namespace UcpAgent.FakeCatalog;

public static class FakeProductGenerator
{
    private static readonly Dictionary<string, string[]> _categorias = new()
    {
        ["eletronicos"] = ["Smartphone", "Notebook", "Tablet", "Fone de Ouvido", "Smartwatch", "Caixa de Som", "Câmera", "Monitor"],
        ["moda"]        = ["Camiseta", "Calça Jeans", "Vestido", "Tênis", "Jaqueta", "Bolsa", "Sandália", "Shorts"],
        ["casa"]        = ["Cafeteira", "Liquidificador", "Aspirador", "Ferro de Passar", "Ventilador", "Fritadeira Air Fryer", "Panela Elétrica"],
        ["beleza"]      = ["Shampoo", "Condicionador", "Perfume", "Protetor Solar", "Hidratante", "Base", "Batom"],
        ["esportes"]    = ["Bicicleta", "Haltere", "Colchonete", "Corda de Pular", "Luva de Boxe", "Tênis Esportivo", "Garrafa Térmica"],
    };

    private static readonly string[] _vendors = [
        "Samsung BR", "LG", "Multilaser", "Philco", "Positivo",
        "Hering", "Riachuelo", "Osklen", "Reserva", "Farm",
        "Mondial", "Arno", "Oster BR", "Fischer", "Britânia",
        "Natura", "O Boticário", "L'Oréal BR", "Nívea BR", "Garnier",
        "Penalty", "Olympikus", "Fila BR", "Speedo BR", "Mormaii"
    ];

    private static readonly string[] _imagens = [
        "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=800",
        "https://images.unsplash.com/photo-1585386959984-a4155224a1ad?w=800",
        "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=800",
        "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=800",
        "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800",
        "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=800",
        "https://images.unsplash.com/photo-1526170375885-4d8ecf77b99f?w=800",
    ];

    public static List<ProductDto> Generate(int qty, string? category = null)
    {
        var faker = new Faker("pt_BR");
        var categorias = category is not null && _categorias.ContainsKey(category.ToLower())
            ? new[] { category.ToLower() }
            : _categorias.Keys.ToArray();

        var produtos = new List<ProductDto>(qty);
        for (var i = 0; i < qty; i++)
        {
            var cat      = faker.PickRandom(categorias);
            var tipo     = faker.PickRandom(_categorias[cat]);
            var vendor   = faker.PickRandom(_vendors);
            var preco    = Math.Round(faker.Random.Decimal(49.90m, 4999.90m), 2);
            var original = Math.Round(preco * faker.Random.Decimal(1.1m, 1.5m), 2);
            var sku      = $"SKU{faker.Random.Int(10000, 99999)}-BR";
            var id       = $"fake-{sku.ToLower()}";

            produtos.Add(new ProductDto(
                Id:                id,
                Title:             $"{tipo} {vendor}",
                Price:             preco,
                ImageUrl:          faker.PickRandom(_imagens),
                Url:               $"https://comprai.example.com/produtos/{id}",
                Category:          cat,
                Source:            "FakeCatalog",
                OriginalPrice:     original,
                AvailableQuantity: faker.Random.Int(10, 500)
            ));
        }
        return produtos;
    }
}
