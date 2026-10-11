using Core.Domain;

namespace Core;

public static class SampleData
{
    public static IEnumerable<Product> Products() =>
    [
        Product.Create("P-001", "SKU-001", "Цемент М400 25кг", "шт", 120),
        Product.Create("P-002", "SKU-002", "Пісок річковий", "т", 35),
        Product.Create("P-003", "SKU-003", "Цегла червона", "шт", 2400),
        Product.Create("P-004", "SKU-004", "Щебінь 5-20", "т", 18),
        Product.Create("P-005", "SKU-005", "Арматура 12мм", "м", 800),
        Product.Create("P-006", "SKU-006", "Гіпсокартон 12.5мм", "шт", 150),
        Product.Create("P-007", "SKU-007", "Профіль CD 60", "шт", 300),
        Product.Create("P-008", "SKU-008", "Фарба біла 10л", "шт", 45),
        Product.Create("P-009", "SKU-009", "Ґрунтовка 5л", "шт", 60),
        Product.Create("P-010", "SKU-010", "Шпаклівка 25кг", "шт", 90),
        Product.Create("P-011", "SKU-011", "Плитковий клей 25кг", "шт", 110),
        Product.Create("P-012", "SKU-012", "Плитка 30x30", "м2", 520),
        Product.Create("P-013", "SKU-013", "Утеплювач 50мм", "м2", 400),
        Product.Create("P-014", "SKU-014", "Саморізи 4x35", "уп", 250),
        Product.Create("P-015", "SKU-015", "Дошка обрізна 50x150", "м3", 12),
    ];
}