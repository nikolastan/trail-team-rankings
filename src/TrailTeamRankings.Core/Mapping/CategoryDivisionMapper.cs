using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Core.Mapping;

/// <summary>
/// Resolves a RunTrace category label to the division it competes in. Only the
/// federation races carry a real junior/senior split; the many general and
/// age-group races (M Gen, "M 40-49", Elite, …) have no juniors. The rule is
/// therefore: anything containing "junior" is <see cref="Division.Juniori"/>;
/// any other <em>recognized</em> competitive category (one we can assign a gender
/// to) is treated as the senior/open division <see cref="Division.Seniori"/>;
/// everything else (relays, "Rekreativci", …) is left unmapped and excluded.
/// See <c>docs/category-mapping.md</c>.
/// </summary>
public static class CategoryDivisionMapper
{
    /// <summary>Attempts to resolve a category to a division.</summary>
    /// <returns>True when the category matched a known division.</returns>
    public static bool TryMap(string? category, out Division division)
    {
        division = default;

        if (string.IsNullOrWhiteSpace(category))
        {
            return false;
        }

        if (category.Trim().ToLowerInvariant().Contains("junior"))
        {
            division = Division.Juniori;
            return true;
        }

        // Any other category we can assign a gender to is a real individual
        // competitive category, treated as the senior/open division.
        if (GenderMapper.Map(category) is not null)
        {
            division = Division.Seniori;
            return true;
        }

        return false;
    }

    /// <summary>Resolves a category to a division, or null when not recognized.</summary>
    public static Division? Map(string? category) =>
        TryMap(category, out var division) ? division : null;
}
