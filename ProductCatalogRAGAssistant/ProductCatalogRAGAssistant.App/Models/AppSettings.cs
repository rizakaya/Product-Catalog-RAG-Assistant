namespace ProductCatalogRAGAssistant.Models;

public sealed class AppSettings
{
    public OllamaSettings Ollama { get; set; } = new();
}

public sealed class OllamaSettings
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string ChatModel { get; set; } = "qwen3.6:latest";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public int TimeoutSeconds { get; set; } = 3;
}
