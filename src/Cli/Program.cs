using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Core.Import;
using Core.Services;
using Core.Storage;

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
    Console.WriteLine();
    Console.WriteLine($"Після всіх відмов: {product}");
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

// ===== Лаб. 3: імпорт тепер під прапорцем --import =====
if (args.Contains("--import"))
{
    string[] importArgs = args.Where(a => a != "--import").ToArray();
    string path = importArgs.Length > 0 ? importArgs[0] : Path.Combine("data", "sample.csv");

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
}

// ===== Лаб. 5: composition root (режим за замовчуванням) =====
bool useFile = args.Contains("--file");
string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

// Єдине місце, де створюються конкретні класи сховища
ICatalogStore store = useFile
    ? new FileCatalogStore(dataPath)
    : new InMemoryCatalogStore(SampleData.Products());

var service = new CatalogService(store);

Console.WriteLine($"Сховище: {store.GetType().Name}");
if (useFile)
    Console.WriteLine($"Файл: {dataPath}");

Console.WriteLine();
Console.WriteLine("=== Сценарій 1: успіх ===");
var created = service.Add("SKU-101", "Кабель UTP cat6", "м", 50);
service.Receive(created.Id, 25);
service.Issue(created.Id, 10);

Console.WriteLine($"Знайдено за id: {service.Find(created.Id)}");
foreach (var p in service.All())
    Console.WriteLine($"  {p.Id} {p.Sku,-10} {p.Name,-22} {p.Quantity,5} {p.Unit}");

Console.WriteLine();
Console.WriteLine("=== Сценарій 2: відмови ===");
TryDo("Receive з неіснуючим id", () => service.Receive("no-such-id", 5));
TryDo("дубль id у сховищі", () => store.Add(Product.Create(created.Id, "SKU-X", "Дубль", "шт", 1)));
TryDo("видача більша за залишок", () => service.Issue(created.Id, 100000));

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