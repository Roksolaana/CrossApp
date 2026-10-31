using Core.Domain;

namespace Core.Reports;

public sealed record DeferredDemoResult(
    int BeforeChange, int AfterChange, int SnapshotCount, int QueryAfterClear);

public static class DeferredExecutionDemo
{
    public static DeferredDemoResult Run(IEnumerable<Product> products)
    {
        var source = products.ToList();
        var query = source.Where(p => p.Quantity < 10);

        int before = query.Count();
        source.Add(Product.Create("tmp00001", "SKU-999", "Скоба", "шт", 3));
        int after = query.Count();

        var snapshot = query.ToList();
        source.Clear();

        return new DeferredDemoResult(before, after, snapshot.Count, query.Count());
    }
}