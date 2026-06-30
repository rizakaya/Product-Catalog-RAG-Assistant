using System.Net.Http.Json;
using System.Text.Json;

namespace ProductCatalogRAGAssistant.Services;

public sealed class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly IEmbeddingService _fallback;
    private readonly string _model;

    public OllamaEmbeddingService(HttpClient httpClient, IEmbeddingService fallback, string model = "nomic-embed-text")
    {
        _httpClient = httpClient;
        _fallback = fallback;
        _model = model;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        return await TryEmbedWithNewApiAsync(text, cancellationToken)
            ?? await TryEmbedWithLegacyApiAsync(text, cancellationToken)
            ?? await _fallback.EmbedAsync(text, cancellationToken);
    }

    private async Task<float[]?> TryEmbedWithNewApiAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "/api/embed",
                new { model = _model, input = text },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var payload = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            if (!payload.RootElement.TryGetProperty("embeddings", out var embeddingsElement))
            {
                return null;
            }

            var firstEmbedding = embeddingsElement.ValueKind == JsonValueKind.Array && embeddingsElement.GetArrayLength() > 0
                ? embeddingsElement[0]
                : default;

            return firstEmbedding.ValueKind == JsonValueKind.Array
                ? firstEmbedding.EnumerateArray().Select(x => x.GetSingle()).ToArray()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<float[]?> TryEmbedWithLegacyApiAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "/api/embeddings",
                new { model = _model, prompt = text },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var payload = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            if (!payload.RootElement.TryGetProperty("embedding", out var embeddingElement))
            {
                return null;
            }

            return embeddingElement.EnumerateArray().Select(x => x.GetSingle()).ToArray();
        }
        catch
        {
            return null;
        }
    }
}
