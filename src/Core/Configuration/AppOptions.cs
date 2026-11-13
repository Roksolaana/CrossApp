namespace Core.Configuration;

public sealed class AppOptions
{
    public string EnvironmentName { get; init; } = "Development";
    public StorageOptions Storage { get; init; } = new();
    public LoggingOptions Logging { get; init; } = new();
    public WarehouseOptions Warehouse { get; init; } = new();
}

public sealed class StorageOptions
{
    public string DataPath { get; init; } = "data";
    public string FileName { get; init; } = "items.json";

    public string ResolveFullPath(string baseDir)
    {
        string root;
        if (string.IsNullOrWhiteSpace(DataPath))
            root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CrossApp");
        else if (Path.IsPathRooted(DataPath))
            root = DataPath;
        else
            root = Path.Combine(baseDir, DataPath);

        Directory.CreateDirectory(root);
        return Path.GetFullPath(Path.Combine(root, FileName));
    }
}

public sealed class LoggingOptions
{
    public string MinimumLevel { get; init; } = "Information";
}

public sealed class WarehouseOptions
{
    public string DefaultUnit { get; init; } = "шт";
}