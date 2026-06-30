namespace ProductCatalogRAGAssistant.Models;

public sealed class Product
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Description { get; set; } = "";
    public List<string> Features { get; set; } = [];

    public string ToSearchText()
    {
        return $"""
        Urun: {Name}
        Kod: {Id}
        Kategori: {Category}
        Fiyat: {Price:0} TL
        Stok: {Stock}
        Aciklama: {Description}
        Ozellikler: {string.Join(", ", Features)}
        """;
    }
}
