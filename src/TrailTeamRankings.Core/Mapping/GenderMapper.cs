using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Core.Mapping;

/// <summary>
/// Resolves a RunTrace category label to the competitor's gender. RunTrace events
/// use several category vocabularies, but across them gender is almost always the
/// leading token — <c>M …</c> / <c>Ž …</c> (e.g. "M Gen", "M 40-49", "Ž Elite").
/// The federation races instead use words ("Seniori"/"Juniori"/"Veterani" for men,
/// the "-ke" ending or "Ž" for women, "Apsolutna M/Ž"). This mapper handles both.
/// See <c>docs/category-mapping.md</c>.
/// </summary>
public static class GenderMapper
{
    // Male word categories (women are detected structurally, see TryMap).
    private static readonly string[] MaleKeywords =
        ["apsolutna m", "seniori", "juniori", "veterani"];

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

        // Female first: the Cyrillic-Latin "ž" ("Apsolutna Ž", "Ž Gen", "Ž 40-49")
        // or the "-ke" feminine ending ("Juniorke", "Veteranke", "Seniorke").
        if (normalized.Contains('ž') || normalized.EndsWith("ke", StringComparison.Ordinal))
        {
            gender = Gender.Female;
            return true;
        }

        // Male: a leading "M" token ("M Gen", "M 40-49", "M Elite") or a known word.
        if (FirstTokenIsM(normalized) || MaleKeywords.Any(normalized.Contains))
        {
            gender = Gender.Male;
            return true;
        }

        return false;
    }

    /// <summary>Resolves a category to a gender, or null when not recognized.</summary>
    public static Gender? Map(string? category) =>
        TryMap(category, out var gender) ? gender : null;

    private static bool FirstTokenIsM(string normalized)
    {
        var space = normalized.IndexOf(' ');
        var firstToken = space < 0 ? normalized : normalized[..space];
        return firstToken == "m";
    }
}
