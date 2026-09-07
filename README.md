# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: **Склад**.
Сутності: Product (товар), StockBatch (партія), Warehouse (склад), Movement (переміщення).
Призначення: облік залишків товарів по партіях на складах.

## Запуск

dotnet build
dotnet run --project src/Cli

## Середовище

.NET SDK 10.0.400, Windows 11/10 x64, RID win-x64

## Додаткове завдання

### Порівняння self-contained публікацій під різні RID

dotnet publish src/Cli -c Release -r win-x64 --self-contained true
dotnet publish src/Cli -c Release -r linux-x64 --self-contained true

Розмір каталогів publish:
- win-x64: 78 MB
- linux-x64: 80 MB

Різниця мінімальна (~2 MB) — код застосунку та BCL однакові для обох платформ,
відрізняється лише упакований у публікацію platform-specific .NET runtime.

### Прапорець --json

dotnet run --project src/Cli -- --json

Виводить ту саму інформацію про середовище одним JSON-рядком (System.Text.Json)
замість форматованої таблиці.

Виявлена проблема: за замовчуванням `JsonSerializer` екранує кирилицю
в `\uXXXX`-послідовності (наприклад, `\u0421\u0435...` замість "Сеньків").

Рішення: використано `JsonSerializerOptions` з
`Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)`, що вимикає
екранування non-ASCII символів і виводить кирилицю як є.