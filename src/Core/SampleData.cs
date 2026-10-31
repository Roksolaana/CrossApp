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
        Product.Create("P-016", "SKU-016", "Скоба будівельна", "уп", 6),
        Product.Create("P-017", "SKU-017", "Герметик силіконовий", "шт", 8),
        Product.Create("P-018", "SKU-018", "Труба ПВХ 50мм", "м", 300),
        Product.Create("P-019", "SKU-019", "Лак паркетний", "л", 7),
        Product.Create("P-020", "SKU-020", "Розчинник 1л", "л", 40),
    ];

    public static IEnumerable<Movement> Movements() =>
    [
        Move("M-001", "P-003", 500, MovementStatus.Confirmed),
        Move("M-002", "P-001", 100),
        Move("M-003", "P-003", 300, MovementStatus.Confirmed),
        Move("M-004", "P-005", 250),
        Move("M-005", "P-012", 120, MovementStatus.Confirmed),
        Move("M-006", "P-001", 40, MovementStatus.Cancelled),
        Move("M-007", "P-018", 60),
        Move("M-008", "P-999", 10),
    ];

    private static Movement Move(string id, string productId, int amount,
        MovementStatus status = MovementStatus.Draft)
    {
        var movement = Movement.Create(id, productId, amount);
        if (status != MovementStatus.Draft)
            movement.TransitionTo(status);
        return movement;
    }
}