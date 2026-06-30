using ProductCatalogRAGAssistant.Models;
using ProductCatalogRAGAssistant.Services;
using ProductCatalogRAGAssistant.Tools;
using System.Text.Json;

var cancellationToken = CancellationToken.None;
var services = CreateServices();

await services.RagService.IndexAsync(cancellationToken);

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("Product Catalog RAG Assistant");
Console.WriteLine("Komutlar: /products, /product P001, /stock P001, /compare P001 P002, /search oyun laptopu, /ask 30 bin TL alti oyun laptopu, /exit");
Console.WriteLine();

while (true)
{
    Console.Write("User > ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    input = input.Trim();

    if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (input.Equals("/products", StringComparison.OrdinalIgnoreCase))
    {
        var products = await services.CatalogService.GetProductsAsync(cancellationToken);
        foreach (var product in products)
        {
            Console.WriteLine($"{product.Id} - {product.Name} | {product.Category} | {product.Price:0} TL | Stok: {product.Stock}");
        }

        continue;
    }

    if (input.StartsWith("/search ", StringComparison.OrdinalIgnoreCase))
    {
        var query = input["/search ".Length..].Trim();
        var (_, searchResults) = await services.RagService.SearchAsync(query, cancellationToken: cancellationToken);
        foreach (var result in searchResults.Select((value, index) => new { value, index }))
        {
            var product = result.value.Document.Product;
            Console.WriteLine($"{result.index + 1}. {product.Id} - {product.Name} | similarity: {result.value.Similarity:0.000} | {product.Price:0} TL | stok {product.Stock}");
        }

        continue;
    }

    if (await services.ToolRouter.TryExecuteAsync(input, cancellationToken) is { } toolOutput)
    {
        Console.WriteLine(toolOutput);
        continue;
    }

    var question = input.StartsWith("/ask ", StringComparison.OrdinalIgnoreCase)
        ? input["/ask ".Length..].Trim()
        : input;

    var (request, results) = await services.RagService.SearchAsync(question, cancellationToken: cancellationToken);
    PrintInternalFlow(request, results);

    Console.Write("Asistan > ");
    await foreach (var part in services.RagService.StreamAnswerAsync(question, results, cancellationToken))
    {
        Console.Write(part);
    }

    Console.WriteLine();
    Console.WriteLine();
}

static AppServices CreateServices()
{
    var appSettings = LoadAppSettings();
    var ollamaSettings = appSettings.Ollama;
    var catalogService = new ProductCatalogService();
    var localEmbedding = new LocalHashEmbeddingService();
    var ollamaHttpClient = new HttpClient
    {
        BaseAddress = new Uri(ReadSetting(ollamaSettings.BaseUrl, "OLLAMA_BASE_URL")),
        Timeout = TimeSpan.FromSeconds(Math.Max(1, ollamaSettings.TimeoutSeconds))
    };

    var embeddingModel = ReadSetting(ollamaSettings.EmbeddingModel, "OLLAMA_EMBEDDING_MODEL");
    var chatModel = ReadSetting(ollamaSettings.ChatModel, "OLLAMA_CHAT_MODEL");
    IEmbeddingService embeddingService = new OllamaEmbeddingService(ollamaHttpClient, localEmbedding, embeddingModel);

    var vectorStore = new InMemoryVectorStore();
    var intentParser = new IntentParserService();
    var chatService = new OllamaChatService(ollamaHttpClient, chatModel);
    var ragService = new RagService(catalogService, embeddingService, vectorStore, intentParser, chatService);

    var searchTool = new SearchProductsTool(ragService);
    var getProductTool = new GetProductByCodeTool(catalogService);
    var checkStockTool = new CheckStockTool(catalogService);
    var compareTool = new CompareProductsTool(catalogService);
    var toolRouter = new ToolRouter(searchTool, getProductTool, checkStockTool, compareTool);

    return new AppServices(catalogService, ragService, toolRouter);
}

static AppSettings LoadAppSettings()
{
    var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    if (!File.Exists(path))
    {
        return new AppSettings();
    }

    var json = File.ReadAllText(path);
    return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions.Default) ?? new AppSettings();
}

static string ReadSetting(string appSettingValue, string environmentVariableName)
{
    return string.IsNullOrWhiteSpace(appSettingValue)
        ? Environment.GetEnvironmentVariable(environmentVariableName) ?? ""
        : appSettingValue;
}

static void PrintInternalFlow(ProductSearchRequest request, IReadOnlyList<VectorSearchResult> results)
{
    Console.WriteLine();
    Console.WriteLine("Intent parse:");
    Console.WriteLine($"  Intent: {request.Intent}");
    Console.WriteLine($"  Category: {request.Category ?? "-"}");
    Console.WriteLine($"  MaxPrice: {(request.MaxPrice is null ? "-" : $"{request.MaxPrice:0} TL")}");
    Console.WriteLine($"  OnlyInStock: {request.OnlyInStock}");
    Console.WriteLine($"  Features: {(request.RequiredFeatures.Count == 0 ? "-" : string.Join(", ", request.RequiredFeatures))}");
    Console.WriteLine();
    Console.WriteLine("Retrieval:");

    if (results.Count == 0)
    {
        Console.WriteLine("  Uygun urun bulunamadi.");
        return;
    }

    foreach (var result in results)
    {
        var product = result.Document.Product;
        Console.WriteLine($"  {product.Id} {product.Name} | similarity {result.Similarity:0.000} | {product.Price:0} TL | stok {product.Stock}");
    }

    Console.WriteLine();
}

internal sealed record AppServices(
    ProductCatalogService CatalogService,
    RagService RagService,
    ToolRouter ToolRouter);
