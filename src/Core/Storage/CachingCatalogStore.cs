using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingCatalogStore(ICatalogStore inner) : ICatalogStore
{
    private readonly ICatalogStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly Dictionary<string, Product> _byId = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<Product>? _list;

    // Лічильники для демонстрації
    public int Hits { get; private set; }
    public int Misses { get; private set; }
    public string InnerName => _inner.GetType().Name;

    public IReadOnlyList<Product> List()
    {
        if (_list is not null)
        {
            Hits++;
            return _list;
        }

        Misses++;
        _list = _inner.List();
        return _list;
    }

    public Product? GetById(string id)
    {
        if (_byId.TryGetValue(id, out var cached))
        {
            Hits++;
            return cached;
        }

        Misses++;
        var found = _inner.GetById(id);
        if (found is not null)
            _byId[found.Id] = found;   // null не кешуємо: запис може з'явитися пізніше
        return found;
    }

    public void Add(Product item)
    {
        _inner.Add(item);
        Invalidate();
    }

    public void Update(Product item)
    {
        _inner.Update(item);
        Invalidate();
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        Invalidate();
        return removed;
    }

    private void Invalidate()
    {
        _list = null;
        _byId.Clear();
    }
}