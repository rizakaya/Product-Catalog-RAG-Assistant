using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ProductCatalogRAGAssistant.Services;

public sealed partial class LocalHashEmbeddingService : IEmbeddingService
{
    private const int Dimensions = 256;

    private static readonly Dictionary<string, string[]> Synonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["oyun"] = ["gaming", "performans", "rtx", "ekran", "karti"],
        ["gaming"] = ["oyun", "performans", "rtx"],
        ["yazilim"] = ["kod", "programlama", "developer", "gelistirme"],
        ["kod"] = ["yazilim", "programlama", "gelistirme"],
        ["laptop"] = ["notebook", "bilgisayar", "dizustu"],
        ["bilgisayar"] = ["laptop", "notebook", "dizustu"],
        ["tv"] = ["televizyon", "film", "dizi", "ekran"],
        ["monitor"] = ["ekran", "4k", "usb-c"],
        ["stok"] = ["adet", "mevcut", "var"],
        ["ucuz"] = ["uygun", "butce", "fiyat"],
        ["uygun"] = ["ucuz", "butce", "fiyat"]
    };

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var vector = new float[Dimensions];
        foreach (var token in Tokenize(text))
        {
            AddToken(vector, token, 1.0f);

            if (Synonyms.TryGetValue(token, out var synonyms))
            {
                foreach (var synonym in synonyms)
                {
                    AddToken(vector, synonym, 0.35f);
                }
            }
        }

        Normalize(vector);
        return Task.FromResult(vector);
    }

    public static string NormalizeText(string text)
    {
        var lower = text.ToLowerInvariant()
            .Replace('ı', 'i')
            .Replace('ğ', 'g')
            .Replace('ü', 'u')
            .Replace('ş', 's')
            .Replace('ö', 'o')
            .Replace('ç', 'c');

        var normalized = lower.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        foreach (Match match in TokenRegex().Matches(NormalizeText(text)))
        {
            var token = match.Value;
            if (token.Length > 1)
            {
                yield return token;
            }
        }
    }

    private static void AddToken(float[] vector, string token, float weight)
    {
        var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(token);
        var index = Math.Abs(hash % Dimensions);
        vector[index] += weight;
    }

    private static void Normalize(float[] vector)
    {
        var length = Math.Sqrt(vector.Sum(v => v * v));
        if (length == 0)
        {
            return;
        }

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = (float)(vector[i] / length);
        }
    }

    [GeneratedRegex("[a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();
}
