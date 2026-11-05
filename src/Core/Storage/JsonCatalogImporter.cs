using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonCatalogImporter : ICatalogImporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<Product>> LoadAsync(
        string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);

        var dtos = await JsonSerializer.DeserializeAsync<List<ProductDto>>(
        stream, Options, ct)
            ?? throw new DomainException(
                $"Файл {Path.GetFileName(path)} не містить списку записів.");
        var result = new List<Product>(dtos.Count);
        var number = 0;

        foreach (var dto in dtos)
        {
            ct.ThrowIfCancellationRequested();
            number++;

            if (dto is null)
                throw new DomainException($"Запис {number}: порожній елемент.");

            try
            {
                result.Add(Product.FromDto(dto));
            }
            catch (ArgumentException ex)
            {
                throw new DomainException($"Запис {number}: {ex.Message}", ex);
            }
        }

        return result;
    }
}