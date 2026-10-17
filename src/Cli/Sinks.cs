using Core.Abstractions;
using Core.Events;

internal static class ChangeFormatter
{
    public static string Format(CatalogChangedEventArgs e) =>
        $"{e.At:HH:mm:ss} {e.Kind,-8} {e.Id} {e.Delta,4:+0;-0} → залишок {e.QuantityAfter}";
}

internal sealed class ConsoleSink : INotificationSink
{
    public void Notify(CatalogChangedEventArgs e) =>
        Console.WriteLine("[console] " + ChangeFormatter.Format(e));
}

internal sealed class FileSink : INotificationSink
{
    private readonly string _path;

    public FileSink(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    }

    public void Notify(CatalogChangedEventArgs e) =>
        File.AppendAllText(_path, ChangeFormatter.Format(e) + Environment.NewLine);
}