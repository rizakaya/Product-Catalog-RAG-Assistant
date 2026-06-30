namespace ProductCatalogRAGAssistant.Models;

public sealed class VectorDocument
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public float[] Embedding { get; set; } = [];
    public Product Product { get; set; } = new();
}

public sealed class VectorSearchResult
{
    public required VectorDocument Document { get; init; }
    public required double Similarity { get; init; }
}
