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
        // Score once (points by rank among eligible finishers); both the team
        // standings and the individual standings read from the same scored set.
        var scored = ChampionshipScorer.Score(field.Runners, division);

        return new DivisionResults
        {
            Division = division,
            TeamStandings = ranking.RankTeams(scored, division),
            MaleIndividuals = BuildIndividuals(scored, Gender.Male),
            FemaleIndividuals = BuildIndividuals(scored, Gender.Female),
            ExcludedRunners = field.Excluded.Where(e => e.Division == division).ToList(),
        };
    }

    private static IReadOnlyList<IndividualResult> BuildIndividuals(
        IEnumerable<ScoredRunner> scored, Gender gender) =>
        scored
            .Where(runner => runner.Gender == gender)
            .OrderBy(runner => runner.Rank)
            .Select(runner => new IndividualResult(runner.Rank, runner.Name, runner.Club, runner.Points))
            .ToList();
}
