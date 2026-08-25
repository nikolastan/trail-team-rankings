using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Text;

namespace TrailTeamRankings.Core.Matching;

/// <summary>
/// Matches a scraped runner to a registered athlete by normalized name, using the
/// club as a tie-breaker when several athletes share a name. Built once from the
/// registry, then queried per runner.
/// </summary>
public sealed class RegistryMatcher
{
    private readonly Dictionary<string, List<RegisteredAthlete>> _byNameKey = [];

    public RegistryMatcher(IEnumerable<RegisteredAthlete> athletes)
    {
        ArgumentNullException.ThrowIfNull(athletes);

        foreach (var athlete in athletes)
        {
            var key = NameNormalizer.Normalize(athlete.FullName);
            if (key.Length == 0)
            {
                continue;
            }

            if (!_byNameKey.TryGetValue(key, out var list))
            {
                _byNameKey[key] = list = [];
            }

            list.Add(athlete);
        }
    }

    /// <summary>
    /// Attempts to find the registered athlete for a scraped name/club. When
    /// several athletes share the name, the one whose club overlaps is preferred;
    /// if that is still ambiguous, the first candidate is returned.
    /// </summary>
    public bool TryMatch(string? name, string? club, out RegisteredAthlete athlete)
    {
        athlete = null!;

        var key = NameNormalizer.Normalize(name);
        if (key.Length == 0 || !_byNameKey.TryGetValue(key, out var candidates))
        {
            return false;
        }

        if (candidates.Count == 1)
        {
            athlete = candidates[0];
            return true;
        }

        var clubKey = NameNormalizer.Normalize(club);
        var byClub = candidates
            .Where(candidate => ClubsOverlap(NameNormalizer.Normalize(candidate.Organization), clubKey))
            .ToList();

        athlete = byClub.Count == 1 ? byClub[0] : candidates[0];
        return true;
    }

    // Two club keys "overlap" when they share a meaningful token (length > 2, so
    // common prefixes like "pk"/"ok" are ignored). Enough to split homonyms.
    private static bool ClubsOverlap(string a, string b)
    {
        if (a.Length == 0 || b.Length == 0)
        {
            return false;
        }

        var tokensA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
        return b.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(token => token.Length > 2 && tokensA.Contains(token));
    }
}
