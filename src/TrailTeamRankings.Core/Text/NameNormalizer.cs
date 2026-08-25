using System.Text;

namespace TrailTeamRankings.Core.Text;

/// <summary>
/// Produces a canonical key for comparing personal names across the two data
/// sources (Cyrillic registry vs Latin RunTrace). The key is script-, case-,
/// diacritic-, punctuation-, and word-order-independent:
/// <list type="number">
/// <item>transliterate Cyrillic → Latin,</item>
/// <item>lower-case and fold diacritics to ASCII (č/ć→c, š→s, ž→z, đ→dj),</item>
/// <item>keep only letters, split into words, sort them, join with single spaces.</item>
/// </list>
/// So "Ђуро Борбељ", "Borbelj Đuro" and "djuro borbelj" all share one key.
/// </summary>
public static class NameNormalizer
{
    public static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var latin = SerbianTransliterator.ToLatin(name).ToLowerInvariant();

        var builder = new StringBuilder(latin.Length + 2);
        foreach (var ch in latin)
        {
            switch (ch)
            {
                case 'č' or 'ć':
                    builder.Append('c');
                    break;
                case 'š':
                    builder.Append('s');
                    break;
                case 'ž':
                    builder.Append('z');
                    break;
                case 'đ':
                    builder.Append("dj");
                    break;
                case >= 'a' and <= 'z':
                    builder.Append(ch);
                    break;
                default:
                    builder.Append(' '); // any separator/punctuation/digit becomes a break
                    break;
            }
        }

        var tokens = builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Array.Sort(tokens, StringComparer.Ordinal);
        return string.Join(' ', tokens);
    }
}
