using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class FileCatalogStore(string path) : ICatalogStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string _path = Path.GetFullPath(path);
    private readonly Dictionary<string, Product> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    public string FilePath => _path;

    public IReadOnlyList<Product> List()
    {
        EnsureLoaded();
        return _cache.Values.ToList();
    }

    public Product? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public void Add(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();

        if (_cache.ContainsKey(item.Id))
            throw new DomainException($"Запис з id={item.Id} уже існує.");

        _cache.Add(item.Id, item);
        Flush();
    }

    public void Update(Product item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        _cache[item.Id] = item;
        Flush();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id))
            return false;

        Flush();
        return true;
    }

    public void ReplaceAll(IEnumerable<Product> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _cache.Clear();
        foreach (var item in items)
            _cache[item.Id] = item;
        _loaded = true;
        Flush();
    }

    private void EnsureLoaded()
    {
        if (_loaded) return;
        if (!File.Exists(_path)) { _loaded = true; return; }

        List<ProductDto?> dtos;
        try
        {
            dtos = JsonSerializer.Deserialize<List<ProductDto?>>(File.ReadAllText(_path), Options) ?? [];
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Файл даних пошкоджений: {_path}. {ex.Message}", ex);
        }

        _cache.Clear();
        var number = 0;
        foreach (var dto in dtos)
        {
            number++;
            if (dto is null)
                throw new InvalidDataException($"Файл даних {_path}: запис {number} порожній.");

            try
            {
                var product = Product.FromDto(dto);
                _cache[product.Id] = product;
            }
            catch (ArgumentException ex)
            {
                throw new InvalidDataException(
                    $"Файл даних {_path}: запис {number} некоректний. {ex.Message}", ex);
            }
        }

        _loaded = true;
    }

    private void Flush()
    {
        var dtos = _cache.Values.Select(product => product.ToDto()).ToList();
        var tmp = _path + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(tmp, JsonSerializer.Serialize(dtos, Options));
        File.Move(tmp, _path, overwrite: true);
    }
}