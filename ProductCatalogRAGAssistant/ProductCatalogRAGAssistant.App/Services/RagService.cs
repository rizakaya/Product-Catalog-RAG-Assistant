using System.Text;
using ProductCatalogRAGAssistant.Models;

namespace ProductCatalogRAGAssistant.Services;

public sealed class RagService
{
    private readonly ProductCatalogService _catalogService;
    private readonly IEmbeddingService _embeddingService;
    private readonly InMemoryVectorStore _vectorStore;
    private readonly IntentParserService _intentParser;
    private readonly OllamaChatService _chatService;
    private bool _indexed;

    public RagService(
        ProductCatalogService catalogService,
        IEmbeddingService embeddingService,
        InMemoryVectorStore vectorStore,
        IntentParserService intentParser,
        OllamaChatService chatService)
    {
        _catalogService = catalogService;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _intentParser = intentParser;
        _chatService = chatService;
    }

    public async Task IndexAsync(CancellationToken cancellationToken = default)
    {
        if (_indexed)
        {
            return;
        }

        var products = await _catalogService.GetProductsAsync(cancellationToken);
        foreach (var product in products)
        {
            var text = product.ToSearchText();
            await _vectorStore.AddAsync(new VectorDocument
            {
                Id = product.Id,
                Text = text,
                Product = product,
                Embedding = await _embeddingService.EmbedAsync(text, cancellationToken)
            }, cancellationToken);
        }

        _indexed = true;
    }

    public async Task<(ProductSearchRequest Request, IReadOnlyList<VectorSearchResult> Results)> SearchAsync(
        string question,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        await IndexAsync(cancellationToken);
        var request = _intentParser.Parse(question);
        var queryEmbedding = await _embeddingService.EmbedAsync(question, cancellationToken);
        var results = await _vectorStore.SearchAsync(queryEmbedding, request, topK, cancellationToken);
        return (request, results);
    }

    public async Task<ProductAssistantResponse> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        var (request, results) = await SearchAsync(question, 5, cancellationToken);
        var answerBuilder = new StringBuilder();
        await foreach (var part in StreamAnswerAsync(question, results, cancellationToken))
        {
            answerBuilder.Append(part);
        }

        return new ProductAssistantResponse
        {
            Answer = answerBuilder.ToString().Trim(),
            RecommendedProducts = results.Select(r => new RecommendedProduct
            {
                Id = r.Document.Product.Id,
                Name = r.Document.Product.Name,
                Price = r.Document.Product.Price,
                Stock = r.Document.Product.Stock,
                Similarity = Math.Round(r.Similarity, 3),
                Reason = BuildReason(r.Document.Product, request)
            }).ToList(),
            Warnings = results.Count == 0 ? ["Katalog verisi disinda urun onerilmedi."] : [],
            MissingInfo = results.Count == 0 ? "Uygun urun bulunamadi." : null
        };
    }

    public async IAsyncEnumerable<string> StreamAnswerAsync(
        string question,
        IReadOnlyList<VectorSearchResult> results,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(question, results);
        await foreach (var part in _chatService.GenerateAsync(prompt, results, cancellationToken))
        {
            yield return part;
        }
    }

    private static string BuildPrompt(string question, IReadOnlyList<VectorSearchResult> results)
    {
        var context = string.Join(Environment.NewLine + Environment.NewLine, results.Select(r =>
            $"""
            Kod: {r.Document.Product.Id}
            Urun: {r.Document.Product.Name}
            Kategori: {r.Document.Product.Category}
            Fiyat: {r.Document.Product.Price:0} TL
            Stok: {r.Document.Product.Stock}
            Benzerlik: {r.Similarity:0.000}
            Aciklama: {r.Document.Product.Description}
            Ozellikler: {string.Join(", ", r.Document.Product.Features)}
            """));

        return $"""
        Sen bir urun katalog asistanisin.
        Sadece asagidaki katalog bilgilerini kullan.
        Katalogda olmayan urun, fiyat veya stok bilgisini uydurma.
        Bilgi yetmiyorsa bunu acikca soyle.

        Kullanici sorusu:
        {question}

        Katalogdan bulunan urunler:
        {context}

        Kisa, net ve gerekceli Turkce cevap ver.
        """;
    }

    private static string BuildReason(Product product, ProductSearchRequest request)
    {
        var reasons = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            reasons.Add($"{request.Category} kategorisinde");
        }

        if (request.MaxPrice is not null)
        {
            reasons.Add($"{request.MaxPrice:0} TL butce altinda");
        }

        foreach (var feature in request.RequiredFeatures)
        {
            if (product.Features.Any(f => f.Contains(feature, StringComparison.OrdinalIgnoreCase)))
            {
                reasons.Add($"{feature} ihtiyaciyla uyumlu");
            }
        }

        return reasons.Count == 0 ? product.Description : string.Join(", ", reasons);
    }
}
