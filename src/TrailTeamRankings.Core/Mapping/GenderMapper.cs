using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Core.Mapping;

/// <summary>
/// Resolves a RunTrace category label to the competitor's gender. Female
/// categories carry a distinctive marker — the Cyrillic-Latin "Ž" (as in
/// "Apsolutna Ž") or the "-ke" ending (as in "Juniorke", "Veteranke"). Anything
/// else recognized as a competing category is male. Matching is keyword based
/// and tolerant of casing and surrounding whitespace.
/// </summary>
public static class GenderMapper
{
    // Recognized male categories; female is detected structurally (see TryMap).
    private static readonly string[] MaleKeywords =
        ["apsolutna m", "juniori", "veterani", "seniori"];

    /// <summary>Attempts to resolve a category to a gender.</summary>
    /// <returns>True when the category matched a known gender.</returns>
    public static bool TryMap(string? category, out Gender gender)
    {
        gender = default;

        if (string.IsNullOrWhiteSpace(category))
        {
            return false;
        }

        var normalized = category.Trim().ToLowerInvariant();

        // Female forms first: they are the distinctive ones.
        if (normalized.Contains('ž') || normalized.EndsWith("ke"))
        {
            gender = Gender.Female;
            return true;
        }

        if (MaleKeywords.Any(normalized.Contains))
        {
            gender = Gender.Male;
            return true;
        }

        return false;
    }

    /// <summary>Resolves a category to a gender, or null when not recognized.</summary>
    public static Gender? Map(string? category) =>
        TryMap(category, out var gender) ? gender : null;
}
