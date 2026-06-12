using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Scoring;

namespace TrailTeamRankings.Core.Ranking;

/// <summary>
/// Computes club team standings for a division by summing the points of each
/// club's top counting males and females. The number of counting runners per
/// gender is defined by <see cref="CountingMaleCount"/> and
/// <see cref="CountingFemaleCount"/>. Points come from <see cref="PointsLadder"/>.
/// </summary>
public sealed class TeamRankingService
{
    public const int CountingMaleCount = 2;
    public const int CountingFemaleCount = 1;

    /// <summary>
    /// Ranks teams within a single division. Only Finished, eligible runners with
    /// a valid place are considered; other rows are ignored.
    /// </summary>
    public IReadOnlyList<TeamStanding> RankTeams(IEnumerable<RaceRunner> runners, Division division)
    {
        ArgumentNullException.ThrowIfNull(runners);

        var standings = runners
            .Where(runner =>
                runner.Division == division &&
                runner is { 
                    Status: RaceStatus.Finished,
                    IsEligible: true, 
                    Place: >= 1 
                })
            .GroupBy(runner => runner.Club, StringComparer.OrdinalIgnoreCase)
            .Select(group => BuildStanding(division, group))
            .ToList();

        standings.Sort(CompareStandings);

        for (var index = 0; index < standings.Count; index++)
        {
            standings[index].Rank = index + 1;
        }

        return standings;
    }

    private static TeamStanding BuildStanding(Division division, IEnumerable<RaceRunner> teamRunners)
    {
        var runners = teamRunners.ToList();

        var males = SelectCounting(runners, Gender.Male, CountingMaleCount);
        var females = SelectCounting(runners, Gender.Female, CountingFemaleCount);

        return new TeamStanding
        {
            Division = division,
            Club = runners[0].Club,
            CountingMales = males,
            CountingFemale = females.FirstOrDefault(),
            IsComplete = males.Count == CountingMaleCount && females.Count == CountingFemaleCount,
        };
    }

    private static List<CountingRunner> SelectCounting(
        IEnumerable<RaceRunner> runners, Gender gender, int count) =>
        runners
            .Where(runner => runner.Gender == gender)
            .Select(runner => new CountingRunner(
                runner.Name,
                runner.Club,
                runner.Gender,
                runner.Place,
                PointsLadder.GetPoints(runner.Place)))
            .OrderByDescending(runner => runner.Points)
            .ThenBy(runner => runner.Place)
            .ThenBy(runner => runner.Name, StringComparer.OrdinalIgnoreCase)
            .Take(count)
            .ToList();

    private static int CompareStandings(TeamStanding a, TeamStanding b)
    {
        // Higher total points ranks first.
        var byTotal = b.TotalPoints.CompareTo(a.TotalPoints);
        if (byTotal != 0)
        {
            return byTotal;
        }

        // Teams that filled every counting slot outrank incomplete ones on a tie.
        var byComplete = b.IsComplete.CompareTo(a.IsComplete);
        if (byComplete != 0)
        {
            return byComplete;
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
