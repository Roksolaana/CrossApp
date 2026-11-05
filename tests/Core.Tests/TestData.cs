using Core.Domain;

namespace Core.Tests;

internal static class TestData
{
    public static IReadOnlyList<Product> Items() =>
    [
        Product.Create("T-1", "SKU-BIG",  "Великий запас",  "шт", 500),
        Product.Create("T-2", "SKU-MID",  "Середній запас", "шт", 50),
        Product.Create("T-3", "SKU-LOW",  "Малий запас",    "шт", 5),
        Product.Create("T-4", "SKU-TINY", "Майже нічого",   "шт", 3),
    ];
}