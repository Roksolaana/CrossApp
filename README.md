# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: **Склад**.
Сутності: Product (товар), StockBatch (партія), Warehouse (склад), Movement (переміщення).
Призначення: облік залишків товарів по партіях на складах.

## Запуск

```bash
dotnet build
dotnet run --project src/Cli
```

## Середовище

.NET SDK 10.0.400, Windows 11/10 x64, RID win-x64

## Додаткове завдання

### Порівняння self-contained публікацій під різні RID

```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained true
dotnet publish src/Cli -c Release -r linux-x64 --self-contained true
```

Розмір каталогів publish:
- win-x64: 78 MB
- linux-x64: 80 MB

Різниця мінімальна (~2 MB) — код застосунку та BCL однакові для обох платформ,
відрізняється лише упакований у публікацію platform-specific .NET runtime.

### Прапорець --json

```bash
dotnet run --project src/Cli -- --json
```

Виводить ту саму інформацію про середовище одним JSON-рядком (System.Text.Json)
замість форматованої таблиці.

Виявлена проблема: за замовчуванням `JsonSerializer` екранує кирилицю
в `\uXXXX`-послідовності (наприклад, `\u0421\u0435...` замість "Сеньків").

Рішення: використано `JsonSerializerOptions` з
`Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)`, що вимикає
екранування non-ASCII символів і виводить кирилицю як є.
## Запуск у Docker-контейнері

Спочатку запустити Docker Desktop. Потім з кореня проєкту (PowerShell):

```powershell
docker run --rm -v ${PWD}:/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet run --project src/Cli
```

Команда підключає поточну папку проєкту як том `/src` всередині контейнера
`mcr.microsoft.com/dotnet/sdk:10.0` (Linux/Ubuntu-образ з встановленим .NET SDK)
і виконує `dotnet run` уже всередині цього ізольованого Linux-середовища —
без встановлення .NET на хост-машині.

### Приклад локального запуску (Windows)

```
CrossApp - практикум з крос-платформного програмування
Студент: Сеньків Роксолана, група ФЕІ-36
----------------------------------------------------------------------
| Параметр               | Значення                                  |
----------------------------------------------------------------------
| OC (OSDescription)     | Microsoft Windows 10.0.26200               |
| OC (Environment)       | Microsoft Windows NT 10.0.26200.0          |
| Архітектура процесу    | X64                                        |
| Версія .NET (CLR)      | 10.0.11                                    |
| Runtime                | .NET 10.0.11                               |
| Каталог застосунку     | C:\CrossApp\src\Cli\bin\Debug\net10.0\     |
| Поточний каталог       | C:\CrossApp                                |
----------------------------------------------------------------------
Предметна область: Склад (товари, партії, залишки, переміщення)
```

### Приклад запуску в контейнері (Ubuntu)

```
CrossApp - практикум з крос-платформного програмування
Студент: Сеньків Роксолана, група ФЕІ-36
----------------------------------------------------------------------
| Параметр               | Значення                                  |
----------------------------------------------------------------------
| OC (OSDescription)     | Ubuntu 24.04.4 LTS                         |
| OC (Environment)       | Unix 6.6.87.2                              |
| Архітектура процесу    | X64                                        |
| Версія .NET (CLR)      | 10.0.12                                    |
| Runtime                | .NET 10.0.12                               |
| Каталог застосунку     | /src/src/Cli/bin/Debug/net10.0/            |
| Поточний каталог       | /src                                       |
----------------------------------------------------------------------
Предметна область: Склад (товари, партії, залишки, переміщення)
```

Той самий код (`Cli.dll`) виконується без перекомпіляції в обох середовищах —
відрізняються лише значення, зчитані з реальної ОС під час виконання
(`RuntimeInformation.OSDescription`, шляхи файлової системи), що і демонструє
крос-платформність .NET на рівні керованого коду (IL + CLR).

## Лабораторна 2: Core + Cli, ProjectReference, multi-targeting, публікація

### Структура solution після роботи

```
CrossApp/
├── CrossApp.slnx
├── README.md
├── .gitignore
└── src/
    ├── Core/
    │   ├── Core.csproj          (TargetFrameworks: net8.0;net10.0)
    │   └── EnvironmentInfo.cs   (namespace Core)
    └── Cli/
        ├── Cli.csproj           (ProjectReference → Core)
        └── Program.cs
```

### Команди, якими додано Core і посилання

```bash
dotnet new classlib -n Core -o src/Core -f net10.0
dotnet sln add src/Core/Core.csproj
dotnet add src/Cli/Cli.csproj reference src/Core/Core.csproj
```

У `src/Cli/Cli.csproj` з'явився рядок:
```xml
<ProjectReference Include="..\Core\Core.csproj" />
```
Видалено шаблонний файл-заглушку `src/Core/Class1.cs`.

`EnvironmentReport` — `record` (дані: результат одного вимірювання середовища). `EnvironmentInfo` — `static class` (поведінка: алгоритм збору цих даних). Логіка винесена в `Core`, бо на тижнях 10 і 12 її використовуватимуть також `Api` і `Web`, а не лише `Cli` — дублювати код у кожному клієнті небажано.

`Program.cs` у `Cli` не містить жодного виклику `RuntimeInformation` — перевірено командою `grep -n "RuntimeInformation" src/Cli/Program.cs` (0 збігів). `Cli` лише викликає `EnvironmentInfo.Collect()` і форматує вивід.

### Запуск

```bash
dotnet build
dotnet run --project src/Cli
```

Вивід:
```
CrossApp - інформація про середовище
Студент: Сеньків Роксолана, група ФЕІ-36
----------------------------------------------------------------------
| Параметр               | Значення                                  |
----------------------------------------------------------------------
| OC (OSDescription)     | Microsoft Windows 10.0.26200              |
| Runtime                | .NET 10.0.11                              |
| Архітектура процесу    | X64                                        |
| RID (визначено)        | win-x64                                   |
| RID (від .NET)         | win-x64                                   |
| Каталог застосунку     | C:\CrossApp\src\Cli\bin\Debug\net10.0\    |
----------------------------------------------------------------------
Предметна область: Склад (товари, партії, залишки, переміщення)
```

RID, визначений вручну (`DetectRid()`), і RID від `RuntimeInformation.RuntimeIdentifier` збіглися — обидва `win-x64`.


### Multi-targeting

`Core.csproj`: `<TargetFrameworks>net8.0;net10.0</TargetFrameworks>` — зібрався успішно під обидва TFM без додаткових встановлень.


### Публікація — порівняння режимів

```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained true
dotnet publish src/Cli -c Release -r win-x64 --self-contained false
```

Розмір заміряно (PowerShell):
```powershell
(Get-ChildItem -Recurse "src\Cli\bin\Release\net10.0\win-x64\publish" | Measure-Object Length -Sum).Sum/1MB
```

| RID | Режим | Розмір publish | Потрібен runtime |
|---|---|---|---|
| win-x64 | self-contained | ~76,68 МБ | ні |
| win-x64 | framework-dependent | ~0,20 МБ | так (.NET 10) |

Різниця — понад 390 разів. Self-contained містить повну копію .NET runtime, framework-dependent — лише код застосунку (`Cli.dll`, `Core.dll`) і метадані залежностей.

### Запуск з каталогу publish (без dotnet run)

```bash
./src/Cli/bin/Release/net10.0/win-x64/publish/Cli.exe
```

Вивід ідентичний, окрім каталогу застосунку — тепер `C:\CrossApp\src\Cli\bin\Release\net10.0\win-x64\publish\` (Release-публікація), що підтверджує коректність `AppContext.BaseDirectory` як динамічного значення.

### Плановане розміщення коду в Core

- `Core/Dto/` — record-типи форматів даних (тиждень 3): `ProductDto`
- `Core/Domain/` — сутності з поведінкою(тиждень 4): `Product`, `StockBatch`, `Warehouse`, `Movement`
- `Core/Storage/` — реалізації сховищ (тиждень 5)

### Додаткове завдання (лабораторна 2)

**PublishSingleFile:**
```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
Каталог publish: 3 файли (`Cli.exe`, `Cli.pdb`, `Core.pdb`) замість десятків окремих `.dll`. Розмір ~70,16 МБ. Запускається коректно.

**PublishTrimmed:**
```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained true -p:PublishTrimmed=true
```
Розмір ~19,37 МБ (майже в 4 рази менше за звичайний self-contained). Build видав попередження `IL2026` про рефлексійну серіалізацію в `JsonSerializer.Serialize`. Попередження підтвердилось на практиці: звичайний режим працює, але `--json` падає з винятком `Reflection-based serialization has been disabled for this application` — trimmer видалив метадані, потрібні для рефлексії, бо не зміг статично довести їх використання.

**Умовна компіляція для multi-targeting:**
```csharp
#if NET10_0_OR_GREATER
    private const string BuildNote = "збірка під net10.0";
#else
    private const string BuildNote = "збірка під net8.0";
#endif
```
Виведено у полі "Примітка збірки": `"збірка під net10.0"` (Cli таргетує net10.0).

**Порівняльна таблиця всіх варіантів публікації:**

| Варіант | Розмір | Файлів | `--json` працює |
|---|---|---|---|
| self-contained | ~76,68 МБ | багато | так |
| framework-dependent | ~0,20 МБ | кілька | так |
| self-contained + SingleFile | ~70,16 МБ | 3 | так |
| self-contained + Trimmed | ~19,37 МБ | менше | ні (падає) |

## Лабораторна 3: Базові типи домену, pattern matching, імпорт CSV/JSON

### Структура, додана цією роботою

```text
CrossApp/
├── data/
│   ├── sample.csv (10+ рядків, з них 3 навмисно пошкоджені)
│   ├── sample_clean.csv (10 коректних рядків — для тесту "чистого" імпорту)
│   ├── sample.json (альтернативний формат вхідних даних)
│   └── sample.txt (демонстрація непідтримуваного розширення)
└── src/
    └── Core/
        ├── Dto/
        │   └── ProductDto.cs (record, namespace Core.Dto)
        ├── ImportResult.cs (record ImportResult<T>, namespace Core.Dto)
        └── Import/
            ├── ProductCsvImporter.cs (namespace Core.Import)
            └── ProductJsonImporter.cs (namespace Core.Import)
```

### Record-типи

```csharp
public record ProductDto(
    string Id, string Sku, string Name, string Unit, int Quantity,
    string? Note = null);

public sealed record ImportResult<T>(IReadOnlyList<T> Items, IReadOnlyList<string> Errors);
```

`Note` — єдине nullable-поле: примітка до товару справді може бути відсутньою, решта полів обов'язкові для повноцінного запису складського обліку.

### Розбір рядка: pattern matching

Метод `ProductCsvImporter.ParseLine` валідує та деструктурує вхідний масив колонок через єдиний `switch expression`:

```csharp
return parts switch
{
    // Патерн властивості + реляційний: відсіює рядки з недостатньою кількістю колонок
    { Length: < 5 } => 
        new ParseFailed("очікую 5 колонок"),

    // List pattern з константами + 'or': виявляє порожній SKU або назву
    [_, "", _, _, _] or [_, _, "", _, _] => 
        new ParseFailed("SKU або назва порожні"),

    // Охоронна умова 'when' + 'out': перевіряє коректність числа
    [.., var qty] when !int.TryParse(qty, out int q) || q < 0 => 
        new ParseFailed($"кількість '{qty}' не є невід'ємним числом"),

    // Іменований list pattern: успішний розбір і деструктуризація одразу в змінні
    [var id, var sku, var name, var unit, var qty] => 
        new ParseOk(new ProductDto(id, sku, name, unit, int.Parse(qty))),

    _ => new ParseFailed("некоректний формат рядка")
};
```

Пошкоджений рядок не перериває імпорт решти файлу — кожен результат повертається через `ParseOk` або `ParseFailed` (закрита ієрархія `sealed record`), а помилки збираються окремо від успішних записів у `ImportResult<T>.Errors`.

### Запуск

```bash
# коректний файл
dotnet run --project src/Cli -- data/sample.csv

# інший формат (JSON)
dotnet run --project src/Cli -- data/sample.json

# неіснуючий файл — не падає, виводить зрозуміле повідомлення
dotnet run --project src/Cli -- data/no_such_file.csv
```

### Приклад виводу (data/sample.csv)

```text
Завантажено записів: 10
P-001 SKU-001 Цемент М400 25кг 120 шт
P-002 SKU-002 Пісок будівельний 18 т
P-003 SKU-003 Цегла червона 4200 шт
P-004 SKU-004 Фарба водоемульсійна 10л 36 шт
P-005 SKU-005 Шпаклівка фінішна 250 кг
Пропущено рядків: 3
! рядок 12: очікую 5 колонок, отримав 4
! рядок 13: кількість 'багато' не є невід'ємним числом
! рядок 14: SKU або назва порожні
Статистика: усього 13 / прийнято 10 / пропущено 3 / помилок 23.1%
```

### Приклад виводу (файл не знайдено)

```text
Файл не знайдено: C:\CrossApp\data\no_such_file.csv
```

Код завершення процесу — `1` (перевірено командою `echo $?`); програма не кидає необроблений виняток.

### Формат вхідного файлу

- Роздільник — `;` (крапка з комою), не конфліктує з комами в назвах товарів.
- Перший рядок може бути заголовком (`id;sku;name;unit;quantity`) — розпізнається й пропускається автоматично, файл без заголовка також обробляється коректно.
- Кодування — UTF-8, читання явно виконується через `File.ReadAllLines(path, Encoding.UTF8)`.

### Виявлена проблема: локалізоване форматування чисел

За замовчуванням `{value:F1}` форматує дробові числа за **поточною культурою ОС**: на українській локалі десятковий розділювач — кома (`23,1`), а не крапка (`23.1`). Це та сама проблема, про яку методичка попереджає для парсингу (`CultureInfo.InvariantCulture`), лише в дзеркальному напрямку — при виведенні результату.

**Рішення:**

```csharp
errorRate.ToString("F1", CultureInfo.InvariantCulture)
```

Тепер вивід не залежить від локалі системи, на якій запускається програма.

### Додаткове завдання: другий імпортер (JSON)

```csharp
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var items = JsonSerializer.Deserialize<List<ProductDto>>(json, options) ?? [];
```

Вибір імпортера за розширенням файлу реалізовано через `switch expression`:

```csharp
ImportResult<ProductDto> result = Path.GetExtension(path).ToLowerInvariant() switch
{
    ".csv" => ProductCsvImporter.Load(path),
    ".json" => ProductJsonImporter.Load(path),
    var ext => throw new NotSupportedException($"Непідтримуване розширення: {ext}")
};
```

На відміну від CSV, пошкоджений JSON не можна розібрати частково — `JsonSerializer.Deserialize` або повертає весь масив, або кидає виняток на весь файл одразу. Це принципова відмінність формату: CSV дозволяє ізолювати пошкоджені рядки один від одного, JSON (як єдина деревоподібна структура) — ні.

### Додаткове завдання: статистика імпорту

```text
Статистика: усього 13 / прийнято 10 / пропущено 3 / помилок 23.1%
```

Обчислюється одним виразом як заготовка під звіти сьомого тижня (LINQ-агрегації).