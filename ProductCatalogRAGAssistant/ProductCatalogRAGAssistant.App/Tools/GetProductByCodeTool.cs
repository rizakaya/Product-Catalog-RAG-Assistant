using ProductCatalogRAGAssistant.Models;
using ProductCatalogRAGAssistant.Services;

namespace ProductCatalogRAGAssistant.Tools;

public sealed class GetProductByCodeTool
{
    private readonly ProductCatalogService _catalogService;

    public GetProductByCodeTool(ProductCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public Task<Product?> ExecuteAsync(string id, CancellationToken cancellationToken = default)
    {
        return _catalogService.GetByIdAsync(id, cancellationToken);
    }
}
