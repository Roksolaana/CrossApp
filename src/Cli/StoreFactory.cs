using Core;
using Core.Abstractions;
using Core.Storage;

internal static class StoreFactory
{
    public static string DataPath { get; } =
        Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

    public static ICatalogStore Create(string[] args)
    {
        bool useFile = args.Contains("--file");
        bool useCache = args.Contains("--cache");

        ICatalogStore store = useFile
            ? new FileCatalogStore(DataPath)
            : new InMemoryCatalogStore(SampleData.Products());

        return useCache ? new CachingCatalogStore(store) : store;
    }
}