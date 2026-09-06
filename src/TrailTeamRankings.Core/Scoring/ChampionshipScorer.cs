using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Core.Scoring;

/// <summary>
/// Awards championship points for a division. Eligible finishers are ranked by
/// gender in finishing order, and points come from the <see cref="PointsLadder"/>
/// applied to that rank. Because ranks are assigned <em>after</em> filtering out
/// non-finishers and ineligible runners, the points compact with no gaps — the
/// n-th eligible finisher scores the n-th ladder value.
/// </summary>
public static class ChampionshipScorer
{
    public static IReadOnlyList<ScoredRunner> Score(IEnumerable<RaceRunner> runners, Division division)
    {
        ArgumentNullException.ThrowIfNull(runners);

        var eligible = runners
            .Where(runner =>
                runner.Division == division &&
                runner is { Status: RaceStatus.Finished, IsEligible: true, Place: >= 1 })
            .ToList();

        var scored = new List<ScoredRunner>();
        foreach (var gender in new[] { Gender.Male, Gender.Female })
        {
            var ordered = eligible
                .Where(runner => runner.Gender == gender)
                .OrderBy(runner => runner.Place)
                .ThenBy(runner => runner.Name, StringComparer.OrdinalIgnoreCase);

            var rank = 0;
            foreach (var runner in ordered)
            {
                rank++;
                scored.Add(new ScoredRunner(
                    runner.Name, runner.Club, gender, rank, PointsLadder.GetPoints(rank)));
            }
        }

        return scored;
    }
}
