using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class ImportTests
{
    [Fact]   // 5) скасування
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new CatalogService(new InMemoryCatalogStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonCatalogImporter(), "any.json", cts.Token));
    }

    [Fact]   // 6) некоректний запис -> DomainException; тимчасовий файл
    public async Task ImportAsync_WithBadRecord_ThrowsDomainException_AndAddsNothing()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, """
            [
              { "Id": "A-1", "Sku": "SKU-A1", "Name": "Добрий", "Unit": "шт", "Quantity": 1 },
              { "Id": "A-2", "Sku": "",       "Name": "Поганий", "Unit": "шт", "Quantity": 2 }
            ]
            """);

        try
        {
            var store = new InMemoryCatalogStore();
            var service = new CatalogService(store);

            var ex = await Assert.ThrowsAsync<DomainException>(
                () => service.ImportAsync(new JsonCatalogImporter(), path));

            Assert.Contains("Запис 2", ex.Message);
            Assert.Empty(store.List());
        }
        finally
        {
            File.Delete(path);
        }
    }
}