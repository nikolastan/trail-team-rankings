using System.Text;

namespace TrailTeamRankings.Core.Text;

/// <summary>
/// Canonical key for grouping club/team names that RunTrace spells
/// inconsistently. Transliterates Cyrillic→Latin, lower-cases, folds diacritics,
/// and keeps only letters/digits separated by single spaces — so
/// "PK Tara Bajina Bašta" and "PK Tara Bajina Basta" collapse to one club.
/// <para>
/// Word order is preserved and location words are deliberately NOT stripped, so
/// distinct-but-similar names such as "PSK Balkan" and "PSK Balkan Beograd" stay
/// separate. Merging those needs a canonical club list (out of scope here).
/// </para>
/// </summary>
public static class ClubNormalizer
{
    public static string Normalize(string? club)
    {
        if (string.IsNullOrWhiteSpace(club))
        {
            return string.Empty;
        }

        var latin = SerbianTransliterator.ToLatin(club).ToLowerInvariant();

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
                case >= '0' and <= '9':
                    builder.Append(ch);
                    break;
                default:
                    builder.Append(' ');
                    break;
            }
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
