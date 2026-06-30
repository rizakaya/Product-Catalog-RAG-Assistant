using ProductCatalogRAGAssistant.Models;
using ProductCatalogRAGAssistant.Services;

namespace ProductCatalogRAGAssistant.Tools;

public sealed class SearchProductsTool
{
    private readonly RagService _ragService;

    public SearchProductsTool(RagService ragService)
    {
        _ragService = ragService;
    }

    public async Task<IReadOnlyList<VectorSearchResult>> ExecuteAsync(string query, CancellationToken cancellationToken = default)
    {
        var (_, results) = await _ragService.SearchAsync(query, cancellationToken: cancellationToken);
        return results;
    }
}
