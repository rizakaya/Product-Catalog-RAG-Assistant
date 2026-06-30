namespace ProductCatalogRAGAssistant.Services;

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
}
