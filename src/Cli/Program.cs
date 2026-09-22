using Core.Dto;
using Core.Import;
using System.Globalization;

if (args.Contains("--mixed"))
{
    string mixedPath = Path.Combine("data", "mixed.csv");
    var (products, warehouses, errors) = MixedImporter.Load(mixedPath);

    Console.WriteLine($"Товарів: {products.Count}, складів: {warehouses.Count}");
    foreach (var p in products)
        Console.WriteLine($"  [P] {p.Id} {p.Sku} {p.Name} {p.Quantity} {p.Unit}");
    foreach (var w in warehouses)
        Console.WriteLine($"  [W] {w.Id} {w.Name} {w.City}");

    if (errors.Count > 0)
    {
        Console.WriteLine($"Помилок: {errors.Count}");
        foreach (var e in errors)
            Console.WriteLine($"  ! {e}");
    }

    return 0;
}

string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

ImportResult<ProductDto> result = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".csv" => ProductCsvImporter.Load(path),
    ".json" => ProductJsonImporter.Load(path),
    var ext => throw new NotSupportedException($"Непідтримуване розширення: {ext}")
};

Console.WriteLine($"Завантажено записів: {result.Items.Count}");
foreach (ProductDto p in result.Items.Take(5))
    Console.WriteLine($"  {p.Id,-6} {p.Sku,-10} {p.Name,-26} {p.Quantity,5} {p.Unit}");

if (result.Errors.Count > 0)
{
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
        Console.WriteLine($"  ! {e}");
}

int total = result.Items.Count + result.Errors.Count;
double errorRate = total == 0 ? 0 : result.Errors.Count * 100.0 / total;
Console.WriteLine($"Статистика: усього {total} / прийнято {result.Items.Count} / пропущено {result.Errors.Count} / помилок {errorRate.ToString("F1", CultureInfo.InvariantCulture)}%");

return 0;