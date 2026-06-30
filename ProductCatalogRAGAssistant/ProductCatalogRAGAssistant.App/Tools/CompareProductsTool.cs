using ProductCatalogRAGAssistant.Models;
using ProductCatalogRAGAssistant.Services;

namespace ProductCatalogRAGAssistant.Tools;

public sealed class CompareProductsTool
{
    private readonly ProductCatalogService _catalogService;

    public CompareProductsTool(ProductCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<IReadOnlyList<Product>> ExecuteAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var products = new List<Product>();
        foreach (var id in ids)
        {
            if (await _catalogService.GetByIdAsync(id, cancellationToken) is { } product)
            {
                products.Add(product);
            }
        }

        return products;
    }
}
