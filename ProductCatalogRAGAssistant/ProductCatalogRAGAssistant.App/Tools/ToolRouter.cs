using System.Text.RegularExpressions;

namespace ProductCatalogRAGAssistant.Tools;

public sealed partial class ToolRouter
{
    private readonly SearchProductsTool _searchProductsTool;
    private readonly GetProductByCodeTool _getProductByCodeTool;
    private readonly CheckStockTool _checkStockTool;
    private readonly CompareProductsTool _compareProductsTool;

    public ToolRouter(
        SearchProductsTool searchProductsTool,
        GetProductByCodeTool getProductByCodeTool,
        CheckStockTool checkStockTool,
        CompareProductsTool compareProductsTool)
    {
        _searchProductsTool = searchProductsTool;
        _getProductByCodeTool = getProductByCodeTool;
        _checkStockTool = checkStockTool;
        _compareProductsTool = compareProductsTool;
    }

    public async Task<string?> TryExecuteAsync(string input, CancellationToken cancellationToken = default)
    {
        if (input.StartsWith("/product ", StringComparison.OrdinalIgnoreCase))
        {
            var id = input["/product ".Length..].Trim();
            var product = await _getProductByCodeTool.ExecuteAsync(id, cancellationToken);
            return product is null
                ? $"{id} kodlu urun bulunamadi."
                : $"{product.Id} - {product.Name} | {product.Category} | {product.Price:0} TL | Stok: {product.Stock}";
        }

        if (input.StartsWith("/stock ", StringComparison.OrdinalIgnoreCase))
        {
            return await _checkStockTool.ExecuteAsync(input["/stock ".Length..].Trim(), cancellationToken);
        }

        if (input.StartsWith("/compare ", StringComparison.OrdinalIgnoreCase))
        {
            var ids = ProductCodeRegex().Matches(input).Select(m => m.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var products = await _compareProductsTool.ExecuteAsync(ids, cancellationToken);
            return products.Count == 0
                ? "Karsilastirma icin urun kodu bulunamadi. Ornek: /compare P001 P002"
                : string.Join(Environment.NewLine, products.Select(p => $"{p.Id} - {p.Name}: {p.Price:0} TL, stok {p.Stock}, {p.Description}"));
        }

        if (input.StartsWith("/search ", StringComparison.OrdinalIgnoreCase))
        {
            var query = input["/search ".Length..].Trim();
            var results = await _searchProductsTool.ExecuteAsync(query, cancellationToken);
            return string.Join(Environment.NewLine, results.Select((r, i) =>
                $"{i + 1}. {r.Document.Product.Id} - {r.Document.Product.Name} | similarity: {r.Similarity:0.000} | {r.Document.Product.Price:0} TL"));
        }

        return null;
    }

    [GeneratedRegex("P\\d{3}", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ProductCodeRegex();
}
