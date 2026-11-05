using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class ProgressTests
{
    private sealed class SyncProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }

    private const string FourRecords = """
        [
          { "Id": "G-1", "Sku": "SKU-G1", "Name": "Один",   "Unit": "шт", "Quantity": 1 },
          { "Id": "G-2", "Sku": "SKU-G2", "Name": "Два",    "Unit": "шт", "Quantity": 2 },
          { "Id": "G-3", "Sku": "SKU-G3", "Name": "Три",    "Unit": "шт", "Quantity": 3 },
          { "Id": "G-4", "Sku": "SKU-G4", "Name": "Чотири", "Unit": "шт", "Quantity": 4 }
        ]
        """;

    [Fact]   // 4 записи → прогрес 25, 50, 75, 100
    public async Task ImportAsync_ReportsProgressInPercents()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, FourRecords);

        try
        {
            var service = new CatalogService(new InMemoryCatalogStore());
            var values = new List<int>();

            await service.ImportAsync(
                new JsonCatalogImporter(), path, CancellationToken.None,
                new SyncProgress<int>(values.Add));

            Assert.Equal(new[] { 25, 50, 75, 100 }, values);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]   // повторний імпорт: записи пропущено, але прогрес усе одно доходить до 100
    public async Task ImportAsync_WhenRecordsAlreadyExist_StillReportsHundredPercent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, FourRecords);

        try
        {
            var service = new CatalogService(new InMemoryCatalogStore());
            await service.ImportAsync(new JsonCatalogImporter(), path);

            var values = new List<int>();
            var added = await service.ImportAsync(
                new JsonCatalogImporter(), path, CancellationToken.None,
                new SyncProgress<int>(values.Add));

            Assert.Equal(0, added);
            Assert.Equal(100, values[^1]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}