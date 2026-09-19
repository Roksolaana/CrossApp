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

            string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

            switch (parts)
            {
                case ["P", var id, var sku, var name, var unit, var qty] when int.TryParse(qty, out int q):
                    products.Add(new ProductDto(id, sku, name, unit, q));
                    break;

                case ["W", var id, var name, var city]:
                    warehouses.Add(new WarehouseDto(id, name, city));
                    break;

                case ["P", ..] or ["W", ..]:
                    errors.Add($"рядок {number}: невірна кількість колонок для цього типу");
                    break;

                default:
                    errors.Add($"рядок {number}: невідомий префікс типу '{parts.FirstOrDefault()}'");
                    break;
            }
        }

        return (products, warehouses, errors);
    }
}