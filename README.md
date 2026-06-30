# Product Catalog RAG Assistant

Bu proje, .NET 8 console uygulamasıyla hazırlanmış öğretici bir **ürün kataloğu RAG asistanıdır**.

Kullanıcı doğal dille ürün sorar, sistem ürün kataloğunda semantic search yapar, fiyat/stok/kategori gibi metadata filtrelerini uygular ve bulunan ürünlere göre cevap üretir. Ollama çalışıyorsa Qwen/Ollama üzerinden cevap alır; Ollama yoksa proje yerel fallback ile yine çalışır.

## Amaç

Bu uygulama şu konuları pratik olarak göstermek için yazıldı:

| Konu | Projede nerede görülür? |
| --- | --- |
| Product Catalog | `Data/products.json` |
| Embedding | `LocalHashEmbeddingService`, `OllamaEmbeddingService` |
| Vector Store | `InMemoryVectorStore` |
| Similarity Search | Cosine similarity ile en yakın ürünleri bulma |
| Metadata Filtering | Kategori, fiyat, stok ve özellik filtreleri |
| RAG | Bulunan ürünleri prompt context olarak kullanma |
| Structured Input | `IntentParserService` ile doğal dili arama isteğine çevirme |
| Structured Output | `ProductAssistantResponse` modeli |
| Tool Calling | Search, product lookup, stock check ve compare tool sınıfları |
| Streaming | Console cevabını parça parça yazdırma |
| Test Senaryoları | `ProductCatalogRAGAssistant.Tests` |

## Proje Yapısı

```text
ProductCatalogRAGAssistant/
  ProductCatalogRAGAssistant.slnx
  ProductCatalogRAGAssistant.App/
    Program.cs
    Data/products.json
    Models/
    Services/
    Tools/
    Prompts/
  ProductCatalogRAGAssistant.Tests/
    Program.cs
README.md
test-readme.md
```

## Çalıştırma

Ana uygulama:

```powershell
cd ProductCatalogRAGAssistant
dotnet run --project ProductCatalogRAGAssistant.App\ProductCatalogRAGAssistant.App.csproj
```

Senaryo testleri:

```powershell
cd ProductCatalogRAGAssistant
dotnet run --project ProductCatalogRAGAssistant.Tests\ProductCatalogRAGAssistant.Tests.csproj
```

Derleme:

```powershell
cd ProductCatalogRAGAssistant
dotnet build ProductCatalogRAGAssistant.slnx
```

## Console Komutları

```text
/products
/product P001
/stock P001
/compare P001 P002
/search oyun laptopu
/ask 30 bin TL alti oyun laptopu
/exit
```

Komut yazmadan doğrudan soru da sorabilirsin:

```text
25 bine kadar yazilim icin laptop oner
```

## Ollama Kullanımı

Uygulama Ollama varsa otomatik olarak `http://localhost:11434` adresine bağlanmayı dener.

Önerilen modeller:

```powershell
ollama pull qwen3:8b
ollama pull nomic-embed-text
```

Not: `qwen3.6:latest` chat modeli olarak kullanılabilir, ancak embedding için ayrı bir embedding modeli kullanmak daha doğru olur. Bu yüzden `OLLAMA_CHAT_MODEL` ve `OLLAMA_EMBEDDING_MODEL` ayrı tutulur.

Daha hafif kullanım için:

```powershell
ollama pull qwen3:4b
```

Model isimleri proje içindeki `appsettings.json` dosyasından değiştirilir:

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "ChatModel": "qwen3.6:latest",
    "EmbeddingModel": "nomic-embed-text",
    "TimeoutSeconds": 3
  }
}
```

Bu değerler boş bırakılırsa uygulama yedek olarak `OLLAMA_BASE_URL`, `OLLAMA_CHAT_MODEL` ve `OLLAMA_EMBEDDING_MODEL` environment variable değerlerine bakar.

Ollama embedding endpoint'i 404 dönerse veya embedding modeli bulunamazsa uygulama kapanmaz. Yerel hash embedding ve template cevap üretimiyle RAG akışını göstermeye devam eder.

## RAG Akışı

Örnek soru:

```text
30 bin TL alti oyun laptopu oner
```

İç akış:

1. `IntentParserService` soruyu yapısal isteğe çevirir.
2. Soru embedding vektörüne çevrilir.
3. `InMemoryVectorStore` cosine similarity ile en yakın ürünleri bulur.
4. Kategori, fiyat, stok ve özellik filtreleri uygulanır.
5. Bulunan ürünler prompt context olarak hazırlanır.
6. Ollama varsa Qwen cevap üretir; yoksa fallback cevap döner.
7. Console cevabı streaming benzeri parça parça gösterir.

## Örnek Ürünler

Katalogda laptop, TV, monitör ve aksesuar ürünleri bulunur. Örnekler:

```text
P001 - Monster Abra A5
P002 - Lenovo IdeaPad Slim 3
P003 - Samsung 55Q60C QLED TV
P006 - Dell UltraSharp U2723QE
P007 - Asus TUF Gaming A15
P008 - HP Pavilion 15
```

Katalog dosyası:

```text
ProductCatalogRAGAssistant/ProductCatalogRAGAssistant.App/Data/products.json
```

## Öğrenme Notu

Bu projede RAG şu şekilde ayrılır:

| Parça | Görev |
| --- | --- |
| LLM | Cevap üretir |
| Embedding Model | Metni vektöre çevirir |
| Vector Store | Vektörleri saklar |
| Similarity Search | Soruya en yakın bilgileri bulur |
| Metadata Filter | Fiyat, stok, kategori gibi kesin kuralları uygular |
| RAG | Bulunan bilgileri LLM context içine koyar |
| Tool Calling | Deterministik işlemleri sınıflarla çalıştırır |

Detaylı kullanım ve test senaryoları için `test-readme.md` dosyasına bakabilirsin.
