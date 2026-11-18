using Core.Domain;
using Core.Reports;

namespace Api.Contracts;

public sealed record ProductDto(string Id, string Sku, string Name, string Unit, int Quantity);

public sealed record LowStockDto(string Sku, string Name, int Quantity, string Unit);

public static class ProductMappings
{
    public static ProductDto ToContract(this Product p) =>
        new(p.Id, p.Sku, p.Name, p.Unit, p.Quantity);

    public static LowStockDto ToContract(this StockLine line) =>
        new(line.Sku, line.Name, line.Quantity, line.Unit);
}