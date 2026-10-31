using Core.Domain;

namespace Core.Reports;

public static class ReportService
{
    public static IReadOnlyList<StockLine> LowStock(IEnumerable<Product> source, int threshold) =>
        source.Where(p => p.Quantity < threshold)
              .OrderBy(p => p.Quantity).ThenBy(p => p.Sku, StringComparer.Ordinal)
              .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit))
              .ToList();

    public static IReadOnlyList<StockLine> TopByQuantity(IEnumerable<Product> source, int n) =>
        source.TopBy(p => p.Quantity, n, p => p.Sku)
              .Select(p => new StockLine(p.Sku, p.Name, p.Quantity, p.Unit))
              .ToList();

    public static IReadOnlyList<GroupSummary> ByUnit(IEnumerable<Product> source) =>
        source.GroupBy(p => p.Unit, StringComparer.OrdinalIgnoreCase)
              .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity)))
              .OrderByDescending(s => s.TotalQuantity)
              .ToList();

    public static IReadOnlyList<GroupSummary> ByUnitQuerySyntax(IEnumerable<Product> source) =>
        (from product in source
         group product by product.Unit into g
         orderby g.Sum(p => p.Quantity) descending
         select new GroupSummary(g.Key, g.Count(), g.Sum(p => p.Quantity)))
        .ToList();

    public static (int Items, int Total, double Average, int Max) Totals(IEnumerable<Product> source)
    {
        var list = source.ToList();
        return list.Count == 0
            ? (0, 0, 0d, 0)
            : (list.Count, list.Sum(p => p.Quantity),
               list.Average(p => p.Quantity), list.Max(p => p.Quantity));
    }

    public static IReadOnlyList<MovementLine> MovementLines(
        IEnumerable<Product> products, IEnumerable<Movement> movements) =>
        products.Join(movements,
                      product => product.Id,
                      movement => movement.ProductId,
                      (product, movement) => new MovementLine(
                          movement.Id, product.Sku, product.Name, movement.Amount, movement.Status.ToString()),
                      StringComparer.OrdinalIgnoreCase)
                .OrderBy(l => l.Sku, StringComparer.Ordinal)
                .ThenBy(l => l.MovementId, StringComparer.Ordinal)
                .ToList();

    public static IReadOnlyList<MovementSummary> MovementTotals(
        IEnumerable<Product> products, IEnumerable<Movement> movements) =>
        BuildMovementSummaries(products, movements)
            .OrderByDescending(s => s.TotalAmount)
            .ThenBy(s => s.Sku, StringComparer.Ordinal)
            .ToList();

    public static IReadOnlyList<MovementSummary> TopMoved(
        IEnumerable<Product> products, IEnumerable<Movement> movements, int n) =>
        BuildMovementSummaries(products, movements)
            .TopBy(s => s.TotalAmount, n, s => s.Sku)
            .ToList();

    private static IEnumerable<MovementSummary> BuildMovementSummaries(
        IEnumerable<Product> products, IEnumerable<Movement> movements) =>
        products.GroupJoin(movements,
                           product => product.Id,
                           movement => movement.ProductId,
                           (product, moves) => new MovementSummary(
                               product.Sku, product.Name, moves.Count(), moves.Sum(m => m.Amount)),
                           StringComparer.OrdinalIgnoreCase);
}