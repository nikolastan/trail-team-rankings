using System.Text;

namespace TrailTeamRankings.Core.Text;

/// <summary>
/// Transliterates Serbian Cyrillic to Gaj's Latin — the alphabet RunTrace uses.
/// The mapping is the standard near 1:1, including the digraph letters
/// (љ→lj, њ→nj, џ→dž). Latin (and any other) input passes through unchanged, so
/// the method is safe to call on names of either script.
/// </summary>
public static class SerbianTransliterator
{
    private static readonly Dictionary<char, string> CyrillicToLatin = new()
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['ђ'] = "đ",
        ['е'] = "e", ['ж'] = "ž", ['з'] = "z", ['и'] = "i", ['ј'] = "j", ['к'] = "k",
        ['л'] = "l", ['љ'] = "lj", ['м'] = "m", ['н'] = "n", ['њ'] = "nj", ['о'] = "o",
        ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['ћ'] = "ć", ['у'] = "u",
        ['ф'] = "f", ['х'] = "h", ['ц'] = "c", ['ч'] = "č", ['џ'] = "dž", ['ш'] = "š",
    };

    /// <summary>Converts any Serbian Cyrillic letters in <paramref name="text"/> to Latin.</summary>
    public static string ToLatin(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length + 4);
        foreach (var ch in text)
        {
            if (CyrillicToLatin.TryGetValue(ch, out var lower))
            {
                builder.Append(lower);
            }
            else if (CyrillicToLatin.TryGetValue(char.ToLowerInvariant(ch), out var mapped))
            {
                builder.Append(Capitalize(mapped)); // uppercase Cyrillic → capitalized Latin
            }
            else
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    private static string Capitalize(string latin) =>
        latin.Length == 1
            ? latin.ToUpperInvariant()
            : char.ToUpperInvariant(latin[0]) + latin[1..];
}
