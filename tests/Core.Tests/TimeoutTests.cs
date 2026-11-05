using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class TimeoutTests
{
    private sealed class NeverEndingImporter : ICatalogImporter
    {
        public async Task<IReadOnlyList<Product>> LoadAsync(
            string path, CancellationToken ct = default)
        {
            await Task.Delay(Timeout.Infinite, ct);
            return [];
        }

        public IAsyncEnumerable<Product> LoadStreamAsync(
            string path, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    [Fact]
    public async Task ImportWithTimeoutAsync_WhenTimeoutFires_ThrowsTimeoutException()
    {
        var service = new CatalogService(new InMemoryCatalogStore());

        await Assert.ThrowsAsync<TimeoutException>(() =>
            service.ImportWithTimeoutAsync(
                new NeverEndingImporter(), "any.json", TimeSpan.FromMilliseconds(50)));
    }

    [Fact]
    public async Task ImportWithTimeoutAsync_WhenUserCancels_ThrowsCancellation_NotTimeout()
    {
        var service = new CatalogService(new InMemoryCatalogStore());
        using var userCts = new CancellationTokenSource();
        userCts.CancelAfter(50);

        var ex = await Record.ExceptionAsync(() =>
            service.ImportWithTimeoutAsync(
                new NeverEndingImporter(), "any.json", TimeSpan.FromSeconds(30), userCts.Token));

        Assert.IsAssignableFrom<OperationCanceledException>(ex);
        Assert.IsNotType<TimeoutException>(ex);
    }
}