namespace Core.Reports;

public static class EnumerableExtensions
{
    public static IEnumerable<T> TopBy<T>(
        this IEnumerable<T> source, Func<T, int> key, int n, Func<T, string>? tieBreaker = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);

        var ordered = source.OrderByDescending(key);
        if (tieBreaker is not null)
            ordered = ordered.ThenBy(tieBreaker, StringComparer.Ordinal);

        return ordered.Take(n);
    }
}