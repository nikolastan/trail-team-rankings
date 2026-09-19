using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Scoring;
using TrailTeamRankings.Core.Text;

namespace TrailTeamRankings.Core.Ranking;

/// <summary>
/// Computes club team standings from already-scored runners: for each club, sum
/// the points of its top <see cref="CountingMaleCount"/> males and
/// <see cref="CountingFemaleCount"/> female(s). The scoring (points by rank among
/// eligible finishers) is done upstream by <see cref="ChampionshipScorer"/>.
/// Complete teams are ranked; incomplete teams (a missing counting slot) are
/// listed after them, unranked.
/// </summary>
public sealed class TeamRankingService
{
    public const int CountingMaleCount = 2;
    public const int CountingFemaleCount = 1;

    /// <summary>
    /// Ranks teams within a division from the division's scored runners. Runners
    /// with no club name cannot be on a club team and are dropped; clubs are
    /// grouped by a normalized key so inconsistent spellings count as one.
    /// </summary>
    public IReadOnlyList<TeamStanding> RankTeams(IEnumerable<ScoredRunner> scoredRunners, Division division)
    {
        ArgumentNullException.ThrowIfNull(scoredRunners);

        var standings = scoredRunners
            .GroupBy(runner => ClubNormalizer.Normalize(runner.Club))
            .Where(group => group.Key.Length > 0)
            .Select(group => BuildStanding(division, group))
            .ToList();

        standings.Sort(CompareStandings);

        // Only complete teams contend for a championship placing, so only they are
        // numbered (1..). Incomplete teams sort after every complete one and are
        // listed unranked (Rank stays 0), matching how the results are published.
        var rank = 0;
        foreach (var standing in standings)
        {
            if (standing.IsComplete)
            {
                standing.Rank = ++rank;
            }
        }

        return standings;
    }

    private static TeamStanding BuildStanding(Division division, IEnumerable<ScoredRunner> teamRunners)
    {
        var runners = teamRunners.ToList();

        var males = SelectCounting(runners, Gender.Male, CountingMaleCount);
        var females = SelectCounting(runners, Gender.Female, CountingFemaleCount);

        return new TeamStanding
        {
            Division = division,
            Club = SelectDisplayName(runners),
            CountingMales = males,
            CountingFemale = females.FirstOrDefault(),
            IsComplete = males.Count == CountingMaleCount && females.Count == CountingFemaleCount,
        };
    }

    // Picks a representative original spelling to display for a grouped club:
    // the most common spelling, then the longest (most descriptive), then ordinal.
    private static string SelectDisplayName(IEnumerable<ScoredRunner> runners) =>
        runners
            .GroupBy(runner => runner.Club)
            .OrderByDescending(group => group.Count())
            .ThenByDescending(group => group.Key.Length)
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .First().Key;

    private static List<ScoredRunner> SelectCounting(
        IEnumerable<ScoredRunner> runners, Gender gender, int count) =>
        runners
            .Where(runner => runner.Gender == gender)
            .OrderByDescending(runner => runner.Points)
            .ThenBy(runner => runner.Rank)
            .ThenBy(runner => runner.Name, StringComparer.OrdinalIgnoreCase)
            .Take(count)
            .ToList();

    private static int CompareStandings(TeamStanding a, TeamStanding b)
    {
        // Complete teams (every counting slot filled) rank ahead of all incomplete
        // teams; incomplete teams are only listed afterwards, never interleaved.
        var byComplete = b.IsComplete.CompareTo(a.IsComplete);
        if (byComplete != 0)
        {
            return byComplete;
        }

        // Within a completeness group, higher total points ranks first.
        var byTotal = b.TotalPoints.CompareTo(a.TotalPoints);
        if (byTotal != 0)
        {
            return byTotal;
        }

        // Then the club with the single best counting result wins.
        var byBest = BestSinglePoints(b).CompareTo(BestSinglePoints(a));
        if (byBest != 0)
        {
            return byBest;
        }

        // Final deterministic fallback so ordering is stable.
        return string.Compare(a.Club, b.Club, StringComparison.OrdinalIgnoreCase);
    }

    private static int BestSinglePoints(TeamStanding standing)
    {
        var best = standing.CountingFemale?.Points ?? 0;
        foreach (var male in standing.CountingMales)
        {
            if (male.Points > best)
            {
                best = male.Points;
            }
        }

        return best;
    }
}
