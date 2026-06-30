namespace ProductCatalogRAGAssistant.Models;

public sealed class ProductAssistantResponse
{
    public string Answer { get; set; } = "";
    public List<RecommendedProduct> RecommendedProducts { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public string? MissingInfo { get; set; }
}

public sealed class RecommendedProduct
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Reason { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public double Similarity { get; set; }
}
