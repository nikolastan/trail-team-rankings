using TrailTeamRankings.Core.Matching;
using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Ranking;
using TrailTeamRankings.Core.Scoring;

namespace TrailTeamRankings.Core.Results;

/// <summary>
/// The end-to-end pipeline: registry + scraped runners → <see cref="RaceResults"/>.
/// Resolves the field (matching + eligibility), then ranks teams and builds the
/// individual standings for each division. Pure — no I/O — so it is fully
/// unit-testable and reused unchanged by the UI and exporters.
/// </summary>
public static class RaceResultsBuilder
{
    public static RaceResults Build(
        IEnumerable<RegisteredAthlete> registry,
        IEnumerable<ScrapedRunner> scrapedRunners,
        DateOnly raceDate,
        string? raceTitle = null,
        PlaceSource placeSource = PlaceSource.CategoryPlace)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(scrapedRunners);

        var field = new RaceFieldResolver(registry, placeSource).Resolve(scrapedRunners, raceDate);
        var ranking = new TeamRankingService();

        return new RaceResults
        {
            Seniori = BuildDivision(Division.Seniori, field, ranking),
            Juniori = BuildDivision(Division.Juniori, field, ranking),
            AllRunners = field.Runners,
            UnclassifiedExcluded = field.Excluded.Where(e => e.Division is null).ToList(),
            RaceTitle = raceTitle,
            RaceDate = raceDate,
            GeneratedAt = DateTimeOffset.Now,
        };
    }

    private static DivisionResults BuildDivision(
        Division division, ResolvedField field, TeamRankingService ranking)
    {
        var eligibleFinishers = field.Runners
            .Where(runner =>
                runner.Division == division &&
                runner.Status == RaceStatus.Finished &&
                runner.IsEligible &&
                runner.Place >= 1)
            .ToList();

        return new DivisionResults
        {
            Division = division,
            TeamStandings = ranking.RankTeams(field.Runners, division),
            MaleIndividuals = BuildIndividuals(eligibleFinishers.Where(r => r.Gender == Gender.Male)),
            FemaleIndividuals = BuildIndividuals(eligibleFinishers.Where(r => r.Gender == Gender.Female)),
            ExcludedRunners = field.Excluded.Where(e => e.Division == division).ToList(),
        };
    }

    // Ranks eligible finishers of one gender by points (then place, then name) and
    // assigns a 1-based placement.
    private static IReadOnlyList<IndividualResult> BuildIndividuals(IEnumerable<RaceRunner> runners) =>
        runners
            .Select(runner => (runner, points: PointsLadder.GetPoints(runner.Place)))
            .OrderByDescending(x => x.points)
            .ThenBy(x => x.runner.Place)
            .ThenBy(x => x.runner.Name, StringComparer.OrdinalIgnoreCase)
            .Select((x, index) => new IndividualResult(index + 1, x.runner.Name, x.runner.Club, x.points))
            .ToList();
}
