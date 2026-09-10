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