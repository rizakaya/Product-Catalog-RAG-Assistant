using ProductCatalogRAGAssistant.Services;
using ProductCatalogRAGAssistant.Tools;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var catalog = new ProductCatalogService(Path.Combine(AppContext.BaseDirectory, "Data", "products.json"));
if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "Data", "products.json")))
{
    catalog = new ProductCatalogService(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ProductCatalogRAGAssistant.App", "Data", "products.json")));
}

var embedding = new LocalHashEmbeddingService();
var vectorStore = new InMemoryVectorStore();
var intentParser = new IntentParserService();
using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:11434"), Timeout = TimeSpan.FromMilliseconds(500) };
var chat = new OllamaChatService(httpClient);
var rag = new RagService(catalog, embedding, vectorStore, intentParser, chat);
await rag.IndexAsync();

var scenarios = new List<(string Name, Func<Task<bool>> Check)>
{
    ("Similarity search oyun laptopu icin Monster Abra A5'i bulur", async () =>
    {
        var (_, results) = await rag.SearchAsync("oyun icin laptop");
        return results.Take(3).Any(r => r.Document.Product.Id == "P001");
    }),
    ("Kategori filtresi TV urunlerini laptop aramasindan cikarir", async () =>
    {
        var (_, results) = await rag.SearchAsync("30 bin TL alti laptop");
        return results.Count > 0 && results.All(r => r.Document.Product.Category == "Laptop");
    }),
    ("Stok filtresi stokta olmayan urunu cikarir", async () =>
    {
        var (_, results) = await rag.SearchAsync("stokta olan aksesuar oner");
        return results.All(r => r.Document.Product.Stock > 0);
    }),
    ("Butce filtresi 30000 TL ustunu cikarir", async () =>
    {
        var (_, results) = await rag.SearchAsync("30 bin TL alti oyun laptopu");
        return results.All(r => r.Document.Product.Price <= 30000);
    }),
    ("Kod ile urun getirme tool'u calisir", async () =>
    {
        var tool = new GetProductByCodeTool(catalog);
        var product = await tool.ExecuteAsync("P001");
        return product?.Name == "Monster Abra A5";
    }),
    ("Stok tool'u stok bilgisini dondurur", async () =>
    {
        var tool = new CheckStockTool(catalog);
        var output = await tool.ExecuteAsync("P001");
        return output.Contains("8");
    }),
    ("Karsilastirma tool'u iki urunu dondurur", async () =>
    {
        var tool = new CompareProductsTool(catalog);
        var products = await tool.ExecuteAsync(["P001", "P002"]);
        return products.Count == 2;
    }),
    ("RAG cevabi katalogdan onerilen urun listesi uretir", async () =>
    {
        var response = await rag.AskAsync("25 bine kadar yazilim icin laptop oner");
        return response.RecommendedProducts.Count > 0 && response.RecommendedProducts.All(p => p.Price <= 25000);
    })
};

var passed = 0;
foreach (var scenario in scenarios)
{
    try
    {
        var ok = await scenario.Check();
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")} - {scenario.Name}");
        if (ok)
        {
            passed++;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FAIL - {scenario.Name}: {ex.Message}");
    }
}

Console.WriteLine();
Console.WriteLine($"{passed}/{scenarios.Count} senaryo basarili.");
Environment.ExitCode = passed == scenarios.Count ? 0 : 1;
