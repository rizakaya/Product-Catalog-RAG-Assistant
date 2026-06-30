namespace ProductCatalogRAGAssistant.Models;

public sealed class ProductSearchRequest
{
    public string Intent { get; set; } = "ProductRecommendation";
    public string Query { get; set; } = "";
    public string? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool OnlyInStock { get; set; }
    public List<string> RequiredFeatures { get; set; } = [];
    public string SortBy { get; set; } = "best_match";
}
