using System.Text.Json;
using ProductCatalogRAGAssistant.Models;

namespace ProductCatalogRAGAssistant.Services;

public sealed class ProductCatalogService
{
    private readonly string _productsPath;
    private IReadOnlyList<Product>? _cache;

    public ProductCatalogService(string? productsPath = null)
    {
        _productsPath = productsPath ?? Path.Combine(AppContext.BaseDirectory, "Data", "products.json");
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache is not null)
        {
            return _cache;
        }

        await using var stream = File.OpenRead(_productsPath);
        var products = await JsonSerializer.DeserializeAsync<List<Product>>(stream, JsonOptions.Default, cancellationToken)
            ?? [];

        _cache = products;
        return _cache;
    }

    public async Task<Product?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var products = await GetProductsAsync(cancellationToken);
        return products.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
    }
}

public static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
}
