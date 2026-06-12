using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Core.Mapping;

/// <summary>
/// Resolves a RunTrace category label (e.g. "Apsolutna M", "Juniorke",
/// "Veterani") to the division it competes in. Matching is keyword based and
/// tolerant of casing and surrounding whitespace.
/// </summary>
public static class CategoryDivisionMapper
{
    private static readonly string[] JuniorKeywords = ["junior"];

    // Veterans are treated as seniors pending mentor confirmation; adjust here
    // if they should form their own division.
    private static readonly string[] SeniorKeywords = ["senior", "apsolutna", "veteran"];

    /// <summary>
    /// Attempts to resolve a category to a division.
    /// </summary>
    /// <returns>True when the category matched a known division.</returns>
    public static bool TryMap(string? category, out Division division)
    {
        division = default;

        if (string.IsNullOrWhiteSpace(category))
        {
            return false;
        }

        var normalized = category.Trim().ToLowerInvariant();

        if (JuniorKeywords.Any(normalized.Contains))
        {
            division = Division.Juniori;
            return true;
        }

        if (SeniorKeywords.Any(normalized.Contains))
        {
            division = Division.Seniori;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Resolves a category to a division, or null when it is not recognized.
    /// </summary>
    public static Division? Map(string? category) =>
        TryMap(category, out var division) ? division : null;
}
