using System.Text;

namespace TrailTeamRankings.Core.Text;

/// <summary>
/// Canonical key for grouping club/team names that RunTrace spells
/// inconsistently. Transliterates Cyrillic→Latin, folds all diacritics, and keeps
/// only letters/digits separated by single spaces — so "PK Tara Bajina Bašta" and
/// "PK Tara Bajina Basta" collapse to one club.
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

        var folded = TextNormalization.Latinize(club);

        var builder = new StringBuilder(folded.Length);
        foreach (var ch in folded)
        {
            builder.Append(ch is (>= 'a' and <= 'z') or (>= '0' and <= '9') ? ch : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
