using Core.Dto;
using Core.Import;
using Core.Domain;

if (args.Contains("--domain"))
{
    Console.WriteLine("=== Сценарій 1: успіх ===");
    Product product = Product.Create("P-001", "sku-001", "Цемент М400 25кг", "шт", 100);
    Console.WriteLine(product);

    product.RegisterArrival(50);
    product.Issue(30);
    Console.WriteLine(product);

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 2: порушення інваріантів ===");

    TryDo("видача більша за залишок", () => product.Issue(1000));
    TryDo("порожній SKU", () => Product.Create("P-002", " ", "Пісок", "т", 10));
    TryDo("від'ємний залишок", () => Product.Create("P-003", "SKU-003", "Цегла", "шт", -5));

    return 0;
}
if (args.Contains("--validate"))
{
    string validatePath = args.Length > 1 ? args[1] : Path.Combine("data", "sample.csv");
    ImportResult<ProductDto> dtoResult = ProductCsvImporter.Load(validatePath);
    ImportResult<Product> domainResult = ProductDomainMapper.FromImportResult(dtoResult);

    Console.WriteLine($"Валідних товарів: {domainResult.Items.Count}");
    foreach (Product p in domainResult.Items.Take(5))
        Console.WriteLine($"  {p}");

    if (domainResult.Errors.Count > 0)
    {
        Console.WriteLine($"Відхилено: {domainResult.Errors.Count}");
        foreach (string e in domainResult.Errors)
            Console.WriteLine($"  ! {e}");
    }

    return 0;
}

if (args.Contains("--warehouse"))
{
    Warehouse warehouse = Warehouse.Create("W-001", "Головний склад", 150);
    Product warehouseProduct = Product.Create("P-001", "SKU-001", "Цемент", "шт", 100);

    WarehouseService.RegisterArrival(warehouse, warehouseProduct, 40);
    Console.WriteLine($"Товар: {warehouseProduct}");
    Console.WriteLine($"Склад: зайнято {warehouse.Occupied} з {warehouse.Capacity}");

    TryDo("перевищення місткості складу", () => WarehouseService.RegisterArrival(warehouse, warehouseProduct, 200));

    return 0;
}

if (args.Contains("--movement"))
{
    Movement movement = Movement.Create("M-001", "P-001", 50);
    Console.WriteLine($"Статус: {movement.Status}");

    movement.TransitionTo(MovementStatus.Confirmed);
    Console.WriteLine($"Статус: {movement.Status}");

    TryDo("скасувати підтверджене", () => movement.TransitionTo(MovementStatus.Cancelled));

    return 0;
}

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
Console.WriteLine($"Статистика: усього {result.Total} / прийнято {result.Items.Count} / пропущено {result.Errors.Count} / помилок {result.ErrorRateFormatted}%");

return 0;

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  {title}: виняток НЕ спрацював — інваріант відсутній!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  {title}: {ex.GetType().Name} — {ex.Message}");
    }
}