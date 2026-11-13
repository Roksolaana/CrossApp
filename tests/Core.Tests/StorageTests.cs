using Core.Configuration;
using Core.Domain;
using Core.Storage;

namespace Core.Tests;

public class StorageTests
{
    // Відносний DataPath будується від переданого базового каталогу, папка створюється.
    [Fact]
    public void ResolveFullPath_RelativePath_IsBuiltFromBaseDirectory()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}");
        try
        {
            var options = new StorageOptions { DataPath = "data", FileName = "items.json" };

            var full = options.ResolveFullPath(baseDir);

            Assert.Equal(Path.Combine(baseDir, "data", "items.json"), full);
            Assert.True(Directory.Exists(Path.Combine(baseDir, "data")));
        }
        finally
        {
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, recursive: true);
        }
    }

    // Файл пишеться в camelCase з кирилицею як є, .tmp не лишається, дані читаються назад.
    [Fact]
    public void FileCatalogStore_WritesReadableJson_AndReadsItBack()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}");
        var file = Path.Combine(dir, "items.json");
        try
        {
            var store = new FileCatalogStore(file);
            store.Add(Product.Create("P-1", "SKU-1", "Цемент", "шт", 40));

            var json = File.ReadAllText(file);
            Assert.Contains("\"sku\"", json);
            Assert.Contains("Цемент", json);
            Assert.False(File.Exists(file + ".tmp"));

            var reloaded = new FileCatalogStore(file);
            Assert.Equal(40, reloaded.GetById("P-1")!.Quantity);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}