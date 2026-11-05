using Core.Domain;
using Core.Dto;
using Core.Import;
using Core.Abstractions;
using Core.Services;
using Core.Storage;
using Core.Events;
using Core.Reports;
using System.Text.Json;
using Core;

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

if (args.Contains("--import"))
{
    string[] importArgs = args.Except(new[] { "--import" }).ToArray();
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

if (args.Contains("--events"))
{
    var eventStore = StoreFactory.Create(args);
    var eventService = new CatalogService(eventStore);

    var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

    void OnCatalogChanged(object? sender, CatalogChangedEventArgs e)
    {
        var line = $"{e.At:HH:mm:ss} {e.Kind,-8} {e.Id} {e.Delta,4:+0;-0} → залишок {e.QuantityAfter}";
        Console.WriteLine(line);
        File.AppendAllText(logPath, line + Environment.NewLine);
    }

    Console.WriteLine($"Сховище: {eventStore.GetType().Name}");
    Console.WriteLine($"Лог: {logPath}");
    Console.WriteLine();

    int changes = 0;
    eventService.Changed += OnCatalogChanged;
    eventService.Changed += (_, _) =>
    {
        changes++;
        Console.WriteLine($"    (лічильник: {changes})");
    };

    Console.WriteLine("=== Сценарій 1: успішні зміни (два підписники) ===");
    var newItem = eventService.Add("SKU-101", "Кабель UTP cat6", "м", 50);
    eventService.Receive(newItem.Id, 25);
    eventService.Issue(newItem.Id, 10);
    Console.WriteLine($"Подій отримано: {changes}");

    Console.WriteLine();
    Console.WriteLine("=== Відмова при активній підписці ===");
    TryDo("Receive з від'ємною кількістю", () => eventService.Receive(newItem.Id, -5));
    Console.WriteLine($"Подій отримано: {changes}");

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 2: після відписки (-=) ===");
    eventService.Changed -= OnCatalogChanged;
    eventService.Receive(newItem.Id, 5);
    Console.WriteLine($"Подій отримано: {changes}");

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 3: відмови (подій немає) ===");
    TryDo("Receive з від'ємною кількістю", () => eventService.Receive(newItem.Id, -5));
    TryDo("Receive з неіснуючим id", () => eventService.Receive("no-such-id", 5));
    TryDo("видача більша за залишок", () => eventService.Issue(newItem.Id, 100000));
    Console.WriteLine($"Подій отримано: {changes}");

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 4: Func замість події (FindAll) ===");
    var low = eventService.FindAll(p => p.Quantity < 50);
    Console.WriteLine($"  залишок менше 50: {low.Count}");
    foreach (var item in low)
        Console.WriteLine($"    {item.Id} {item.Sku,-10} {item.Name,-22} {item.Quantity,5} {item.Unit}");

    return 0;
}

if (args.Contains("--sinks"))
{
    var sinkLogPath = Path.Combine(AppContext.BaseDirectory, "logs", "sinks.log");
    INotificationSink[] sinks = [new ConsoleSink(), new FileSink(sinkLogPath)];

    var sinkStore = StoreFactory.Create(args);
    var sinkService = new CatalogService(sinkStore, sinks);

    Console.WriteLine($"Сховище: {sinkStore.GetType().Name}");
    Console.WriteLine($"Файл логу: {sinkLogPath}");
    Console.WriteLine();

    Console.WriteLine("=== Сценарій 1: успішні зміни (ConsoleSink + FileSink) ===");
    var sinkItem = sinkService.Add("SKU-201", "Розетка подвійна", "шт", 40);
    sinkService.Receive(sinkItem.Id, 10);
    sinkService.Issue(sinkItem.Id, 5);

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 2: відмови (рядків sink-ів немає) ===");
    TryDo("Receive з від'ємною кількістю", () => sinkService.Receive(sinkItem.Id, -5));
    TryDo("Receive з неіснуючим id", () => sinkService.Receive("no-such-id", 5));
    TryDo("видача більша за залишок", () => sinkService.Issue(sinkItem.Id, 100000));

    Console.WriteLine();
    Console.WriteLine("=== Сценарій 3: видалення (подія Removed) ===");
    sinkService.Remove(sinkItem.Id);

    return 0;
}

if (args.Contains("--import-report"))
{
    string[] reportArgs = args.Except(new[] { "--import-report" }).ToArray();
    string reportPath = reportArgs.Length > 0 ? reportArgs[0] : Path.Combine("data", "sample.csv");

    if (!File.Exists(reportPath))
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(reportPath)}");
        return 1;
    }

    int skipped = 0;
    var reportResult = ImportWithReport.Load(reportPath, message =>
    {
        skipped++;
        Console.WriteLine($"  пропущено #{skipped}: {message}");
    });

    Console.WriteLine($"Прийнято: {reportResult.Items.Count}, пропущено: {skipped}");

    var collected = new List<string>();
    ImportWithReport.Load(reportPath, collected.Add);
    Console.WriteLine($"Той самий метод з іншим Action: зібрано повідомлень {collected.Count}");

    return 0;
}
if (args.Contains("--import-async"))
{
    var impArgs = args.Where(a => a != "--import-async").ToList();

    int impCancelAfterMs = 0;
    int impIdx = impArgs.IndexOf("--cancel-after");
    if (impIdx >= 0)
    {
        if (impIdx + 1 < impArgs.Count)
            int.TryParse(impArgs[impIdx + 1], out impCancelAfterMs);
        impArgs.RemoveRange(impIdx, Math.Min(2, impArgs.Count - impIdx));
    }

    string impPath = impArgs.Count > 0 ? impArgs[0] : Path.Combine("data", "sample.json");

    var impService = new CatalogService(new InMemoryCatalogStore());
    var impImporter = new JsonCatalogImporter();

    using var impCts = new CancellationTokenSource();

    ConsoleCancelEventHandler impOnCancel = (_, e) =>
    {
        e.Cancel = true;
        impCts.Cancel();
        Console.WriteLine("Отримано Ctrl+C, скасовую імпорт...");
    };
    Console.CancelKeyPress += impOnCancel;

    if (impCancelAfterMs > 0)
        impCts.CancelAfter(impCancelAfterMs);

    try
    {
        int impCount = await impService.ImportAsync(impImporter, impPath, impCts.Token);
        Console.WriteLine($"Імпортовано записів: {impCount}");

        int impAgain = await impService.ImportAsync(impImporter, impPath, impCts.Token);
        Console.WriteLine($"Повторний імпорт того самого файлу: {impAgain}");

        Console.WriteLine($"Усього в каталозі: {impService.All().Count}");
        return 0;
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"Імпорт скасовано. У каталозі встигло з'явитись: {impService.All().Count}");
        return 2;
    }
    catch (DomainException ex)
    {
        Console.WriteLine($"Дані відхилено: {ex.Message}");
        return 3;
    }
    catch (FileNotFoundException)
    {
        Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(impPath)}");
        return 1;
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Некоректний JSON (технічна помилка): {ex.Message}");
        return 4;
    }
    catch (IOException ex)
    {
        Console.WriteLine($"Помилка читання файлу (технічна): {ex.Message}");
        return 5;
    }
    finally
    {
        Console.CancelKeyPress -= impOnCancel;
    }
}

if (args.Any(a => a.StartsWith("report-")))
{
    var reportStore = StoreFactory.Create(args);
    var reportService = new CatalogService(reportStore);
    var reportItems = reportService.All();
    var command = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "report-top";

    switch (command)
    {
        case "report-top":
            PrintLines(ReportService.TopByQuantity(reportItems, 5));
            break;

        case "report-low":
            PrintLines(ReportService.LowStock(reportItems, 10));
            break;

        case "report-groups":
            foreach (var g in ReportService.ByUnit(reportItems))
                Console.WriteLine($"  {g.Key,-6} позицій: {g.Count,3} сумарно: {g.TotalQuantity,6}");
            break;

        case "report-sum":
            {
                var totals = ReportService.Totals(reportItems);
                Console.WriteLine($"  позицій {totals.Items}, всього {totals.Total}, середнє {totals.Average:F2}, макс {totals.Max}");
                break;
            }

        case "report-lazy":
            {
                var demo = DeferredExecutionDemo.Run(reportItems);
                Console.WriteLine($"  Дефіцитних до змін: {demo.BeforeChange}");
                Console.WriteLine($"  Дефіцитних після змін: {demo.AfterChange}");
                Console.WriteLine($"  У знімку лишилось: {demo.SnapshotCount}");
                Console.WriteLine($"  Запит після очищення джерела: {demo.QueryAfterClear}");
                break;
            }

        case "report-empty":
            {
                var none = Array.Empty<Product>();
                var emptyTotals = ReportService.Totals(none);
                Console.WriteLine($"  report-low: {ReportService.LowStock(none, 10).Count} рядків");
                Console.WriteLine($"  report-top: {ReportService.TopByQuantity(none, 5).Count} рядків");
                Console.WriteLine($"  report-groups: {ReportService.ByUnit(none).Count} груп");
                Console.WriteLine($"  report-sum: позицій {emptyTotals.Items}, всього {emptyTotals.Total}, середнє {emptyTotals.Average:F2}, макс {emptyTotals.Max}");
                break;
            }
        case "report-moves":
            {
                var movements = Core.SampleData.Movements();

                Console.WriteLine("  Join: переміщення разом з товарами");
                foreach (var line in ReportService.MovementLines(reportItems, movements))
                    Console.WriteLine($"    {line.MovementId,-6} {line.Sku,-10} {line.Name,-22} {line.Amount,5} {line.Status}");

                Console.WriteLine();
                Console.WriteLine("  GroupJoin: топ-3 товари за сумою переміщень");
                foreach (var item in ReportService.TopMoved(reportItems, movements, 3))
                    Console.WriteLine($"    {item.Sku,-10} {item.Name,-22} переміщень: {item.MovementsCount,2} сумарно: {item.TotalAmount,5}");

                Console.WriteLine();
                Console.WriteLine($"  Товарів у GroupJoin: {ReportService.MovementTotals(reportItems, movements).Count} (Join дав лише ті, що мають переміщення)");
                break;
            }

        case "report-query":
            {
                var byMethod = ReportService.ByUnit(reportItems);
                var byQuery = ReportService.ByUnitQuerySyntax(reportItems);

                Console.WriteLine("  Методний синтаксис:");
                foreach (var g in byMethod)
                    Console.WriteLine($"    {g.Key,-6} позицій: {g.Count,3} сумарно: {g.TotalQuantity,6}");

                Console.WriteLine("  Синтаксис запитів:");
                foreach (var g in byQuery)
                    Console.WriteLine($"    {g.Key,-6} позицій: {g.Count,3} сумарно: {g.TotalQuantity,6}");

                Console.WriteLine($"  Результати однакові: {byMethod.SequenceEqual(byQuery)}");
                break;
            }

        case "report-bench":
            {
                Console.WriteLine("  Генерація 100000 записів і вимірювання (середнє з кількох повторів)...");
                var rows = LinqBenchmark.Run();
                foreach (var r in rows)
                    Console.WriteLine($"  {r.Name,-42} {r.AverageMs,9:F2} мс   контрольна сума: {r.Checksum}");

                Console.WriteLine($"  Варіанти 1 і 2 дали однаковий результат: {rows[0].Checksum == rows[1].Checksum}");
                Console.WriteLine($"  Варіанти 3 і 4 дали однаковий результат: {rows[2].Checksum == rows[3].Checksum}");
                break;
            }
        default:
            Console.WriteLine("report-top | report-low | report-groups | report-sum | report-lazy | report-empty | report-moves | report-query | report-bench");
            break;
    }

    return 0;
}

ICatalogStore store = StoreFactory.Create(args);
var service = new CatalogService(store);

Console.WriteLine($"Сховище: {Describe(store)}");
if (args.Contains("--file"))
    Console.WriteLine($"Файл: {StoreFactory.DataPath}");

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

Console.WriteLine();
Console.WriteLine("=== Сценарій 3: пошук (Func<Product, bool>) ===");
Show("залишок менше 50", x => x.Quantity < 50);
Show("назва містить 'каб'", x => x.Name.Contains("каб", StringComparison.OrdinalIgnoreCase));
Show("одиниця = 'м'", x => x.Unit == "м");

if (store is CachingCatalogStore cached)
{
    Console.WriteLine();
    Console.WriteLine("=== Сценарій 4: кеш (декоратор) ===");

    store.Update(created);
    int hits0 = cached.Hits, misses0 = cached.Misses;
    service.All();
    service.All();
    service.All();
    Console.WriteLine($"  3 виклики All(): до внутрішнього сховища = {cached.Misses - misses0}, з кешу = {cached.Hits - hits0}");

    store.Update(created);
    hits0 = cached.Hits; misses0 = cached.Misses;
    service.All();
    Console.WriteLine($"  All() після зміни: до внутрішнього сховища = {cached.Misses - misses0}, з кешу = {cached.Hits - hits0}");
}

return 0;

void Show(string title, Func<Product, bool> predicate)
{
    var found = service.FindAll(predicate);
    Console.WriteLine($"  {title}: {found.Count}");
    foreach (var item in found)
        Console.WriteLine($"    {item.Id} {item.Sku,-10} {item.Name,-22} {item.Quantity,5} {item.Unit}");
}

static string Describe(ICatalogStore s) =>
    s is CachingCatalogStore c
        ? $"{nameof(CachingCatalogStore)} -> {c.InnerName}"
        : s.GetType().Name;

static void PrintLines(IReadOnlyList<StockLine> lines)
{
    foreach (var l in lines)
        Console.WriteLine($"  {l.Sku,-10} {l.Name,-22} {l.Quantity,6} {l.Unit}");
}

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