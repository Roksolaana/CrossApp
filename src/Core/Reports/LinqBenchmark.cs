using System.Diagnostics;
using Core.Domain;

namespace Core.Reports;

public sealed record BenchmarkRow(string Name, double AverageMs, long Checksum);

public static class LinqBenchmark
{
    private const int Size = 100_000;
    private const int Repeats = 5;
    private static readonly string[] Units = ["шт", "м", "м2", "м3", "уп", "т", "л"];

    public static IReadOnlyList<BenchmarkRow> Run()
    {
        var data = Generate(Size);

        return new[]
        {
            Measure("1. Один ланцюжок, без ToList усередині", () => OneChain(data)),
            Measure("2. ToList усередині ланцюжка", () => WithMiddleToList(data)),
            Measure("3. Запит двічі, без матеріалізації", () => QueryTwice(data)),
            Measure("4. Запит один раз + ToList", () => MaterializedOnce(data)),
        };
    }

    private static List<Product> Generate(int count)
    {
        var random = new Random(42);
        var list = new List<Product>(count);
        for (int i = 0; i < count; i++)
        {
            list.Add(Product.Create(
                $"G-{i}", $"SKU-{i:D6}", $"Товар {i}",
                Units[random.Next(Units.Length)], random.Next(0, 1000)));
        }
        return list;
    }

    private static BenchmarkRow Measure(string name, Func<long> action)
    {
        long checksum = action();               // прогрів: JIT і кеші, цей запуск не рахуємо
        double total = 0;
        for (int i = 0; i < Repeats; i++)
        {
            var sw = Stopwatch.StartNew();
            checksum = action();
            sw.Stop();
            total += sw.Elapsed.TotalMilliseconds;
        }
        return new BenchmarkRow(name, total / Repeats, checksum);
    }

    // Варіанти 1 і 2: той самий звіт, у другому додатковий проміжний ToList
    private static long OneChain(IEnumerable<Product> data) =>
        Checksum(data.Where(p => p.Quantity < 500)
                     .OrderBy(p => p.Quantity).ThenBy(p => p.Sku, StringComparer.Ordinal)
                     .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit))
                     .ToList());

    private static long WithMiddleToList(IEnumerable<Product> data) =>
        Checksum(data.Where(p => p.Quantity < 500).ToList()
                     .OrderBy(p => p.Quantity).ThenBy(p => p.Sku, StringComparer.Ordinal)
                     .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit))
                     .ToList());

    // Варіанти 3 і 4: результат потрібен двічі (кількість і сума)
    private static long QueryTwice(IEnumerable<Product> data)
    {
        var query = data.Where(p => p.Quantity < 100)
                        .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit));
        int count = query.Count();                       // 1-й прохід по джерелу
        long sum = query.Sum(l => (long)l.Quantity);     // 2-й прохід по джерелу
        return count * 1_000_003L + sum;
    }

    private static long MaterializedOnce(IEnumerable<Product> data)
    {
        var list = data.Where(p => p.Quantity < 100)
                       .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit))
                       .ToList();                        // запит виконано один раз
        int count = list.Count;
        long sum = list.Sum(l => (long)l.Quantity);
        return count * 1_000_003L + sum;
    }

    private static long Checksum(IReadOnlyList<StockLine> lines) =>
        lines.Count * 1_000_003L + lines.Sum(l => (long)l.Quantity);
}