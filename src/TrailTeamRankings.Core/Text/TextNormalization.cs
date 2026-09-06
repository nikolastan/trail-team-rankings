using System.Globalization;
using System.Text;

namespace TrailTeamRankings.Core.Text;

/// <summary>
/// Shared text folding used by the name and club normalizers. Transliterates
/// Cyrillic → Latin, then strips <em>every</em> diacritic to its ASCII base
/// letter (á→a, č→c, ü→u, é→e, …) and lower-cases. This is stronger than folding
/// only the Serbian set: foreign names (e.g. "Máté") no longer lose letters.
/// </summary>
internal static class TextNormalization
{
    /// <summary>
    /// Cyrillic → Latin, diacritics folded, lower-cased. Non-letter characters are
    /// preserved for the caller to tokenize. "đ"/"Đ" become "dj" (they do not
    /// decompose under Unicode normalization).
    /// </summary>
    public static string Latinize(string text)
    {
        var latin = SerbianTransliterator.ToLatin(text)
            .Replace("đ", "dj")
            .Replace("Đ", "Dj");

        var decomposed = latin.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue; // drop the combining accent left by decomposition
            }

            builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }
}
