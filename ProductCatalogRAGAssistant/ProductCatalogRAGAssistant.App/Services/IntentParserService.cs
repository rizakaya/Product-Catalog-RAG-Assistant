using System.Text.RegularExpressions;
using ProductCatalogRAGAssistant.Models;

namespace ProductCatalogRAGAssistant.Services;

public sealed partial class IntentParserService
{
    public ProductSearchRequest Parse(string input)
    {
        var normalized = LocalHashEmbeddingService.NormalizeText(input);
        var request = new ProductSearchRequest
        {
            Query = input.Trim(),
            OnlyInStock = normalized.Contains("stok") || normalized.Contains("var mi") || normalized.Contains("mevcut")
        };

        if (ContainsAny(normalized, "laptop", "notebook", "bilgisayar", "dizustu"))
        {
            request.Category = "Laptop";
        }
        else if (ContainsAny(normalized, "tv", "televizyon"))
        {
            request.Category = "TV";
        }
        else if (ContainsAny(normalized, "monitor", "ekran"))
        {
            request.Category = "Monitor";
        }
        else if (ContainsAny(normalized, "mouse", "aksesuar"))
        {
            request.Category = "Aksesuar";
        }

        if (ContainsAny(normalized, "oyun", "gaming"))
        {
            request.RequiredFeatures.Add("oyun");
        }

        if (ContainsAny(normalized, "yazilim", "kod", "programlama", "developer"))
        {
            request.RequiredFeatures.Add("yazilim");
        }

        if (TryReadPrice(normalized, out var price, out var isMax))
        {
            if (isMax)
            {
                request.MaxPrice = price;
            }
            else
            {
                request.MinPrice = price;
            }
        }

        if (ContainsAny(normalized, "karsilastir", "kiyasla", "fark"))
        {
            request.Intent = "CompareProducts";
        }
        else if (ContainsAny(normalized, "stok", "var mi", "mevcut"))
        {
            request.Intent = "CheckStock";
        }

        return request;
    }

    private static bool ContainsAny(string text, params string[] words)
    {
        return words.Any(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryReadPrice(string text, out decimal price, out bool isMax)
    {
        foreach (Match match in PriceRegex().Matches(text))
        {
            var number = decimal.Parse(match.Groups["number"].Value.Replace(".", ""));
            var unit = match.Groups["unit"].Value;
            price = unit is "bin" or "k" ? number * 1000 : number;
            var after = text[Math.Min(match.Index + match.Length, text.Length)..];
            var before = text[..match.Index];
            isMax = after.Contains("alti") || after.Contains("kadar") || before.Contains("en fazla") || before.Contains("max");
            return true;
        }

        price = 0;
        isMax = true;
        return false;
    }

    [GeneratedRegex("(?<number>\\d+[\\.]?\\d*)\\s*(?<unit>bin|k|tl)?", RegexOptions.Compiled)]
    private static partial Regex PriceRegex();
}
