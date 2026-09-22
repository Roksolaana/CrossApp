using Core.Dto;

namespace Core.Import;

public static class MixedImporter
{
    private const char Separator = ';';

    public static (List<ProductDto> Products, List<WarehouseDto> Warehouses, List<string> Errors) Load(string path)
    {
        var products = new List<ProductDto>();
        var warehouses = new List<WarehouseDto>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path, System.Text.Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
                continue;

            switch (ParseLine(line))
            {
                case ParseProductOk p:
                    products.Add(p.Value);
                    break;
                case ParseWarehouseOk w:
                    warehouses.Add(w.Value);
                    break;
                case ParseFailed f:
                    errors.Add($"рядок {number}: {f.Reason}");
                    break;
            }
        }

        return (products, warehouses, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["P", var id, var sku, var name, var unit, var qty] when int.TryParse(qty, out int q) 
                => new ParseProductOk(new ProductDto(id, sku, name, unit, q)),
                
            ["W", var id, var name, var city] 
                => new ParseWarehouseOk(new WarehouseDto(id, name, city)),
                
            ["P", ..] or ["W", ..] 
                => new ParseFailed("невірна кількість колонок для цього типу"),
                
            _ => new ParseFailed($"невідомий префікс типу '{parts.FirstOrDefault()}'")
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseProductOk(ProductDto Value) : ParseOutcome;
    private sealed record ParseWarehouseOk(WarehouseDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}