using Core.Dto;

namespace Core.Import;

public static class ImportWithReport
{
    public static ImportResult<ProductDto> Load(string path, Action<string> onSkipped)
    {
        ArgumentNullException.ThrowIfNull(onSkipped);

        ImportResult<ProductDto> result = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".csv" => ProductCsvImporter.Load(path),
            ".json" => ProductJsonImporter.Load(path),
            var ext => throw new NotSupportedException($"Непідтримуване розширення: {ext}")
        };

        foreach (string error in result.Errors)
            onSkipped(error);

        return result;
    }
}