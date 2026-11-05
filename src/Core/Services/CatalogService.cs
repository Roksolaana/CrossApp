using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

public sealed class CatalogService(ICatalogStore store, IEnumerable<INotificationSink>? sinks = null)
{
    private readonly ICatalogStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly List<INotificationSink> _sinks = sinks?.ToList() ?? [];

    public event EventHandler<CatalogChangedEventArgs>? Changed;

    private void OnChanged(Product p, CatalogChangeKind kind, int delta, int? quantityAfter = null)
    {
        var args = new CatalogChangedEventArgs(p.Id, kind, delta, quantityAfter ?? p.Quantity);
        Changed?.Invoke(this, args);
        foreach (var sink in _sinks)
            sink.Notify(args);
    }

    public Product Add(string sku, string name, string unit, int quantity)
    {
        var product = Product.Create(Guid.NewGuid().ToString("N")[..8], sku, name, unit, quantity);
        _store.Add(product);
        OnChanged(product, CatalogChangeKind.Added, quantity);
        return product;
    }

        public async Task<int> ImportAsync(
        ICatalogImporter importer, string path,
        CancellationToken ct = default, IProgress<int>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ct.ThrowIfCancellationRequested();

        var items = await importer.LoadAsync(path, ct);

        var imported = 0;
        var processed = 0;
        var lastPercent = -1;

        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();

            if (_store.GetById(item.Id) is null)
            {
                _store.Add(item);
                OnChanged(item, CatalogChangeKind.Added, item.Quantity);
                imported++;
            }

            processed++;
            var percent = processed * 100 / items.Count;
            if (percent != lastPercent)
            {
                lastPercent = percent;
                progress?.Report(percent);
            }
        }

        return imported;
    }

    public async Task<int> ImportStreamAsync(
        ICatalogImporter importer, string path, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(importer);

        var imported = 0;
        await foreach (var item in importer.LoadStreamAsync(path).WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();

            if (_store.GetById(item.Id) is not null)
                continue;

            _store.Add(item);
            OnChanged(item, CatalogChangeKind.Added, item.Quantity);
            imported++;
        }

        return imported;
    }

        public async Task<int> ImportWithTimeoutAsync(
        ICatalogImporter importer, string path, TimeSpan timeout,
        CancellationToken userToken = default, IProgress<int>? progress = null)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            userToken, timeoutCts.Token);

        try
        {
            return await ImportAsync(importer, path, linked.Token, progress);
        }
        catch (OperationCanceledException)
            when (timeoutCts.IsCancellationRequested && !userToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Імпорт перевищив ліміт часу {timeout.TotalMilliseconds:0} мс.");
        }
    }
    
    public void Receive(string id, int quantity)
    {
        var product = _store.GetById(id)
            ?? throw new DomainException($"Немає запису з id={id}.");

        product.RegisterArrival(quantity);
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Received, quantity);
    }

    public void Issue(string id, int quantity)
    {
        var product = _store.GetById(id)
            ?? throw new DomainException($"Немає запису з id={id}.");

        product.Issue(quantity);
        _store.Update(product);
        OnChanged(product, CatalogChangeKind.Issued, -quantity);
    }

    public bool Remove(string id)
    {
        var product = _store.GetById(id);
        if (product is null || !_store.Remove(id))
            return false;

        OnChanged(product, CatalogChangeKind.Removed, -product.Quantity, 0);
        return true;
    }

    public IReadOnlyList<Product> All() => _store.List();

    public Product? Find(string id) => _store.GetById(id);

    public IReadOnlyList<Product> FindAll(Func<Product, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }
}