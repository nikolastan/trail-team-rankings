using System.Text;

namespace TrailTeamRankings.Core.Text;

/// <summary>
/// Produces a canonical key for comparing personal names across the two data
/// sources (Cyrillic registry vs Latin RunTrace). The key is script-, case-,
/// diacritic-, punctuation-, and word-order-independent:
/// <list type="number">
/// <item>transliterate Cyrillic → Latin and fold all diacritics to ASCII,</item>
/// <item>keep only letters, split into words, sort them, join with single spaces.</item>
/// </list>
/// So "Ђуро Борбељ", "Borbelj Đuro", "djuro borbelj" — and now "Máté" vs "Mate" —
/// all share one key.
/// </summary>
public static class NameNormalizer
{
    public static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var folded = TextNormalization.Latinize(name);

        var builder = new StringBuilder(folded.Length);
        foreach (var ch in folded)
        {
            builder.Append(ch is >= 'a' and <= 'z' ? ch : ' ');
        }

        var tokens = builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Array.Sort(tokens, StringComparer.Ordinal);
        return string.Join(' ', tokens);
    }
}
