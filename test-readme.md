# Product Catalog RAG Assistant - Test ve Senaryo Rehberi

Bu dosya projeyi daha detaylı anlaman için hazırlanmış senaryo rehberidir. Her senaryo, RAG veya tool calling tarafındaki belirli bir davranışı gözlemletir.

## Hızlı Test

```powershell
cd ProductCatalogRAGAssistant
dotnet run --project ProductCatalogRAGAssistant.Tests\ProductCatalogRAGAssistant.Tests.csproj
```

Beklenen sonuç:

```text
8/8 senaryo basarili.
```

## Temel Console Senaryoları

### 1. Ürünleri Listeleme

Komut:

```text
/products
```

Beklenen davranış:

Tüm katalog ürünleri kod, ad, kategori, fiyat ve stok bilgisiyle listelenir.

Öğrettiği konu:

JSON katalog okuma ve domain model.

### 2. Ürün Koduyla Detay Getirme

Komut:

```text
/product P001
```

Beklenen davranış:

`P001 - Monster Abra A5` ürünü fiyat ve stok bilgisiyle gösterilir.

Öğrettiği konu:

Tool calling mantığında deterministic lookup.

### 3. Stok Sorgulama

Komut:

```text
/stock P001
```

Beklenen davranış:

Monster Abra A5 için stok adedi döner.

Komut:

```text
/stock P999
```

Beklenen davranış:

Ürünün katalogda bulunamadığı söylenir.

### 4. Ürün Karşılaştırma

Komut:

```text
/compare P001 P002
```

Beklenen davranış:

Monster Abra A5 ve Lenovo IdeaPad Slim 3 fiyat, stok ve açıklama bilgileriyle yan yana incelenebilir şekilde listelenir.

Öğrettiği konu:

Tool ile birden fazla ürünü deterministik biçimde çekme.

## Similarity Search Senaryoları

### 5. Oyun Laptopu Arama

Komut:

```text
/search oyun laptopu
```

Beklenen davranış:

Monster Abra A5 üst sıralarda gelir. Asus TUF Gaming A15 de oyun odaklı olduğu için sonuçlarda görünebilir.

Öğrettiği konu:

Embedding ve cosine similarity.

### 6. Yazılım İçin Laptop Arama

Komut:

```text
/search yazilim icin laptop
```

Beklenen davranış:

HP Pavilion 15, Monster Abra A5, MacBook Air M2 gibi yazılım/laptop bağlantısı güçlü ürünler gelir.

Öğrettiği konu:

Semantic search sadece kelime eşleşmesi değildir; açıklama ve özellik alanları birlikte değerlendirilir.

### 7. TV Arama

Komut:

```text
/search film izlemek icin buyuk ekran
```

Beklenen davranış:

Samsung 55Q60C QLED TV üst sıralarda gelir.

Öğrettiği konu:

Kullanıcı `TV` demese bile anlam yakınlığıyla doğru kategori bulunabilir.

## Metadata Filter Senaryoları

### 8. Bütçe Filtresi

Komut:

```text
/ask 30 bin TL alti oyun laptopu oner
```

Beklenen davranış:

Monster Abra A5 önerilir. Asus TUF Gaming A15 oyun için güçlü olsa da 30.000 TL üzerinde olduğu için elenir.

Öğrettiği konu:

Vector search tek başına yetmez; fiyat gibi kesin kurallar metadata filter ile uygulanır.

### 9. Kategori Filtresi

Komut:

```text
/ask 30 bin TL alti laptop oner
```

Beklenen davranış:

TV, monitör ve aksesuar ürünleri gelmez. Sadece laptop kategorisi değerlendirilir.

Öğrettiği konu:

Kategori filtresi retrieval sonucunu temizler.

### 10. Stok Filtresi

Komut:

```text
/ask stokta olan aksesuar oner
```

Beklenen davranış:

Logitech MX Master 3S stokta olmadığı için önerilmez. Uygun stoklu aksesuar yoksa sistem bunu açıkça belirtir.

Öğrettiği konu:

`onlyInStock` filtresi stok `0` olan ürünleri çıkarır.

## RAG Cevap Senaryoları

### 11. Doğrudan Soru

Girdi:

```text
25 bine kadar yazilim icin laptop oner
```

Beklenen davranış:

HP Pavilion 15 veya Lenovo IdeaPad Slim 3 gibi bütçeye uyan laptoplar değerlendirilir. Cevap katalogdaki ürünlerden üretilir.

Öğrettiği konu:

Komut yazmadan doğal dil sorusu RAG akışına girer.

### 12. Katalog Dışı Ürün İsteği

Girdi:

```text
MacBook Pro M5 onerir misin?
```

Beklenen davranış:

Katalogda MacBook Pro M5 yoksa sistem bu ürünü uydurmamalıdır. Yakın ürün bulursa katalogdaki ürün olduğunu belirtmelidir.

Öğrettiği konu:

RAG prompt kontrolü: katalog dışı bilgi uydurmama.

### 13. Eksik Bilgi

Girdi:

```text
su gecirmez telefon oner
```

Beklenen davranış:

Katalogda telefon olmadığı için uygun ürün bulunamadığı söylenir veya sonuçlar zayıf kalır.

Öğrettiği konu:

RAG sistemi her soruya ürün uydurmak zorunda değildir.

## Structured Input Senaryoları

### 14. Fiyat ve Kategori Çıkarma

Girdi:

```text
30 bin TL alti laptop
```

Beklenen parse:

```json
{
  "category": "Laptop",
  "maxPrice": 30000,
  "onlyInStock": false
}
```

### 15. Stok ve Özellik Çıkarma

Girdi:

```text
stokta olan oyun laptopu oner
```

Beklenen parse:

```json
{
  "category": "Laptop",
  "requiredFeatures": ["oyun"],
  "onlyInStock": true
}
```

### 16. Yazılım Kullanımı Çıkarma

Girdi:

```text
yazilim gelistirme icin bilgisayar bakiyorum
```

Beklenen parse:

```json
{
  "category": "Laptop",
  "requiredFeatures": ["yazilim"]
}
```

## Tool Calling Senaryoları

### 17. SearchProducts Tool

Komut:

```text
/search oyun laptopu
```

Beklenen tool:

```json
{
  "toolName": "SearchProducts",
  "arguments": {
    "query": "oyun laptopu"
  }
}
```

### 18. GetProductByCode Tool

Komut:

```text
/product P001
```

Beklenen tool:

```json
{
  "toolName": "GetProductByCode",
  "arguments": {
    "id": "P001"
  }
}
```

### 19. CheckStock Tool

Komut:

```text
/stock P001
```

Beklenen tool:

```json
{
  "toolName": "CheckStock",
  "arguments": {
    "id": "P001"
  }
}
```

### 20. CompareProducts Tool

Komut:

```text
/compare P001 P002
```

Beklenen tool:

```json
{
  "toolName": "CompareProducts",
  "arguments": {
    "productIds": ["P001", "P002"]
  }
}
```

## Otomatik Testlerin Kapsadığı Davranışlar

`ProductCatalogRAGAssistant.Tests` şu kontrolleri yapar:

1. Oyun laptopu araması Monster Abra A5'i ilk sonuçlarda bulur.
2. Laptop aramasında TV ürünleri elenir.
3. Stok filtresi stokta olmayan ürünleri çıkarır.
4. 30.000 TL bütçe filtresi daha pahalı ürünleri çıkarır.
5. Kod ile ürün getirme tool'u çalışır.
6. Stok tool'u doğru stok bilgisini döndürür.
7. Karşılaştırma tool'u iki ürünü getirir.
8. RAG cevabı önerilen ürün listesini structured output olarak üretir.

## Geliştirme İçin Yeni Senaryo Fikirleri

İleride şu davranışlar eklenebilir:

1. Kullanıcının önceki tercihini memory içinde tutma.
2. Son mesajları history olarak prompt'a ekleme.
3. JSON structured output parse hatalarında retry mekanizması.
4. Qdrant veya SQLite vector extension ile kalıcı vector store.
5. API katmanı ekleyip frontend ile kullanma.
6. Gerçek LLM tool-calling kararını Ollama prompt'u üzerinden üretme.
7. Ürün yorumları, puanları ve garanti bilgisiyle daha zengin katalog.
