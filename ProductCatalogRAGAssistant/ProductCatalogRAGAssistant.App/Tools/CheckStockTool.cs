using ProductCatalogRAGAssistant.Services;

namespace ProductCatalogRAGAssistant.Tools;

public sealed class CheckStockTool
{
    private readonly ProductCatalogService _catalogService;

    public CheckStockTool(ProductCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public async Task<string> ExecuteAsync(string id, CancellationToken cancellationToken = default)
    {
        var product = await _catalogService.GetByIdAsync(id, cancellationToken);
        return product is null
            ? $"{id} kodlu urun katalogda bulunamadi."
            : $"{product.Name} stok durumu: {product.Stock} adet.";
    }
}
