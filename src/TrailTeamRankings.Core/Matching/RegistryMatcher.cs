using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Text;

namespace TrailTeamRankings.Core.Matching;

/// <summary>
/// Matches a scraped runner to a registered athlete. Two passes:
/// <list type="number">
/// <item><b>Exact</b> — normalized name key equality (club breaks homonym ties).</item>
/// <item><b>Fuzzy fallback</b> — only when exact fails, and only when the club
/// agrees: a name that is a superset of a registry name (an inserted nickname,
/// e.g. "Aleksandra <i>Coka</i> Mijanović") or differs from it by a single small
/// typo (a spelling variant, e.g. "Victoria" vs "Viktorija"). The fallback is
/// rejected if more than one athlete qualifies, to avoid guessing.</item>
/// </list>
/// </summary>
public sealed class RegistryMatcher
{
    private sealed record Entry(RegisteredAthlete Athlete, string[] NameTokens, string ClubKey);

    private readonly Dictionary<string, List<RegisteredAthlete>> _byNameKey = [];
    private readonly List<Entry> _entries = [];

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
            _entries.Add(new Entry(
                athlete,
                key.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                ClubNormalizer.Normalize(athlete.Organization)));
        }
    }

    /// <summary>Attempts to find the registered athlete for a scraped name/club.</summary>
    public bool TryMatch(string? name, string? club, out RegisteredAthlete athlete)
    {
        athlete = null!;

        var key = NameNormalizer.Normalize(name);
        if (key.Length == 0)
        {
            return false;
        }

        // Pass 1 — exact normalized name.
        if (_byNameKey.TryGetValue(key, out var candidates))
        {
            athlete = ResolveExact(candidates, club);
            return true;
        }

        // Pass 2 — club-gated fuzzy / subset fallback.
        return TryFuzzyMatch(key, club, out athlete);
    }

    private static RegisteredAthlete ResolveExact(List<RegisteredAthlete> candidates, string? club)
    {
        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        var clubKey = ClubNormalizer.Normalize(club);
        var byClub = candidates
            .Where(candidate => ClubsOverlap(ClubNormalizer.Normalize(candidate.Organization), clubKey))
            .ToList();

        return byClub.Count == 1 ? byClub[0] : candidates[0];
    }

    private bool TryFuzzyMatch(string key, string? club, out RegisteredAthlete athlete)
    {
        athlete = null!;

        var clubKey = ClubNormalizer.Normalize(club);
        if (clubKey.Length == 0)
        {
            return false; // a corroborating club is required for any non-exact match
        }

        var runnerTokens = key.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (runnerTokens.Length < 2)
        {
            return false; // need at least a first and last name
        }

        RegisteredAthlete? found = null;
        foreach (var entry in _entries)
        {
            if (!ClubsOverlap(entry.ClubKey, clubKey) || !IsCloseName(runnerTokens, entry.NameTokens))
            {
                continue;
            }

            if (found is null)
            {
                found = entry.Athlete;
            }
            else if (!ReferenceEquals(found, entry.Athlete))
            {
                return false; // ambiguous — refuse to guess
            }
        }

        if (found is null)
        {
            return false;
        }

        athlete = found;
        return true;
    }

    // A name is "close" if one token set is a superset of the other (an extra
    // nickname/middle name) or they have the same words with a single small typo.
    private static bool IsCloseName(string[] a, string[] b)
    {
        var (small, large) = a.Length <= b.Length ? (a, b) : (b, a);
        if (small.Length >= 2 && small.All(large.Contains))
        {
            return true;
        }

        if (a.Length != b.Length || a.Length < 2)
        {
            return false;
        }

        int exact = 0, fuzzy = 0, distance = 0;
        var prefixOk = true;
        for (var i = 0; i < a.Length; i++)
        {
            var d = Levenshtein(a[i], b[i]);
            if (d == 0)
            {
                exact++;
            }
            else
            {
                fuzzy++;
                distance += d;
                // Spelling variants share a prefix ("victoria"/"viktorija");
                // different names ("marko"/"darko") do not — reject those.
                if (CommonPrefixLength(a[i], b[i]) < 2)
                {
                    prefixOk = false;
                }
            }
        }

        return exact >= 1 && fuzzy <= 1 && distance <= 2 && prefixOk;
    }

    private static int CommonPrefixLength(string s, string t)
    {
        var max = Math.Min(s.Length, t.Length);
        var i = 0;
        while (i < max && s[i] == t[i])
        {
            i++;
        }

        return i;
    }

    // Two club keys overlap when they share a meaningful token (length > 2, so
    // common prefixes like "pk"/"ok" are ignored).
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

    private static int Levenshtein(string s, string t)
    {
        if (s == t)
        {
            return 0;
        }

        var previous = new int[t.Length + 1];
        for (var j = 0; j <= t.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= s.Length; i++)
        {
            var current = new int[t.Length + 1];
            current[0] = i;
            for (var j = 1; j <= t.Length; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }

            previous = current;
        }

        return previous[t.Length];
    }
}
