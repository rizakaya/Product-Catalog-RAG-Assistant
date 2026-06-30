using ProductCatalogRAGAssistant.Models;

namespace ProductCatalogRAGAssistant.Services;

public sealed class InMemoryVectorStore
{
    private readonly List<VectorDocument> _documents = [];

    public Task AddAsync(VectorDocument document, CancellationToken cancellationToken = default)
    {
        _documents.RemoveAll(x => x.Id == document.Id);
        _documents.Add(document);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        float[] queryEmbedding,
        ProductSearchRequest request,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var results = _documents
            .Where(d => MatchesMetadata(d.Product, request))
            .Select(d => new VectorSearchResult
            {
                Document = d,
                Similarity = CosineSimilarity(queryEmbedding, d.Embedding)
            })
            .OrderByDescending(x => x.Similarity)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<VectorSearchResult>>(results);
    }

    public static double CosineSimilarity(float[] left, float[] right)
    {
        var count = Math.Min(left.Length, right.Length);
        if (count == 0)
        {
            return 0;
        }

        double dot = 0;
        double leftLength = 0;
        double rightLength = 0;

        for (var i = 0; i < count; i++)
        {
            dot += left[i] * right[i];
            leftLength += left[i] * left[i];
            rightLength += right[i] * right[i];
        }

        if (leftLength == 0 || rightLength == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(leftLength) * Math.Sqrt(rightLength));
    }

    private static bool MatchesMetadata(Product product, ProductSearchRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Category) &&
            !string.Equals(product.Category, request.Category, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (request.MinPrice is not null && product.Price < request.MinPrice)
        {
            return false;
        }

        if (request.MaxPrice is not null && product.Price > request.MaxPrice)
        {
            return false;
        }

        if (request.OnlyInStock && product.Stock <= 0)
        {
            return false;
        }

        if (request.RequiredFeatures.Count > 0)
        {
            var text = LocalHashEmbeddingService.NormalizeText($"{product.Name} {product.Description} {string.Join(' ', product.Features)}");
            return request.RequiredFeatures.All(f => text.Contains(LocalHashEmbeddingService.NormalizeText(f), StringComparison.OrdinalIgnoreCase));
        }

        return true;
    }
}
