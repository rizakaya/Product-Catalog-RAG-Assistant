using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ProductCatalogRAGAssistant.Models;

namespace ProductCatalogRAGAssistant.Services;

public sealed class OllamaChatService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public OllamaChatService(HttpClient httpClient, string model = "qwen3.6:latest")
    {
        _httpClient = httpClient;
        _model = model;
    }

    public async IAsyncEnumerable<string> GenerateAsync(
        string prompt,
        IReadOnlyList<VectorSearchResult> retrievedProducts,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (await TryGenerateWithOllamaAsync(prompt, cancellationToken) is { } ollamaAnswer)
        {
            foreach (var word in ollamaAnswer.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                yield return word + " ";
                await Task.Delay(15, cancellationToken);
            }

            yield break;
        }

        var fallback = BuildFallbackAnswer(retrievedProducts);
        foreach (var word in fallback.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            yield return word + " ";
            await Task.Delay(15, cancellationToken);
        }
    }

    private async Task<string?> TryGenerateWithOllamaAsync(string prompt, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "/api/generate",
                new { model = _model, prompt, stream = false },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            return payload.RootElement.TryGetProperty("response", out var responseElement)
                ? responseElement.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string BuildFallbackAnswer(IReadOnlyList<VectorSearchResult> retrievedProducts)
    {
        if (retrievedProducts.Count == 0)
        {
            return "Katalogda bu istege uygun urun bulamadim. Fiyat, kategori veya stok filtresini genisletmeyi deneyebilirsin.";
        }

        var best = retrievedProducts[0].Document.Product;
        var alternatives = retrievedProducts.Skip(1).Take(2).Select(r => r.Document.Product.Name).ToList();
        var answer = $"{best.Price:0} TL fiyatli {best.Name} en uygun aday gorunuyor. {best.Description} Stok: {best.Stock} adet.";
        if (alternatives.Count > 0)
        {
            answer += $" Alternatif olarak {string.Join(", ", alternatives)} incelenebilir.";
        }

        return answer;
    }
}
