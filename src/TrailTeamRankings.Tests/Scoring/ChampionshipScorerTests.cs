using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Scoring;

namespace TrailTeamRankings.Tests.Scoring;

public class ChampionshipScorerTests
{
    private static RaceRunner Runner(
        Gender gender,
        int place,
        string club = "Club",
        Division division = Division.Seniori,
        RaceStatus status = RaceStatus.Finished,
        bool eligible = true,
        string? name = null) =>
        new(name ?? $"{gender}-{place}", club, gender, division, place, status, eligible);

    [Fact]
    public void Score_AwardsLadderPointsByRankAmongEligible()
    {
        var scored = ChampionshipScorer.Score(
            [Runner(Gender.Male, 1), Runner(Gender.Male, 2), Runner(Gender.Male, 3)],
            Division.Seniori);

        Assert.Equal([100, 88, 78], scored.Select(s => s.Points));
        Assert.Equal([1, 2, 3], scored.Select(s => s.Rank));
    }

    [Fact]
    public void Score_CompactsRanks_IgnoringExcludedPlaces()
    {
        // Places 1 and 3 finish; place 2 does not → the two eligible get ranks 1 and 2.
        var scored = ChampionshipScorer.Score(
            [
                Runner(Gender.Male, 1),
                Runner(Gender.Male, 2, status: RaceStatus.Dnf),
                Runner(Gender.Male, 3),
            ],
            Division.Seniori);

        Assert.Equal([100, 88], scored.Select(s => s.Points)); // no gap left by the excluded place-2
    }

    [Fact]
    public void Score_ExcludesIneligibleAndNonFinishers()
    {
        var scored = ChampionshipScorer.Score(
            [
                Runner(Gender.Male, 1, eligible: false),
                Runner(Gender.Male, 2, status: RaceStatus.Dns),
                Runner(Gender.Male, 3),
            ],
            Division.Seniori);

        Assert.Equal(100, Assert.Single(scored).Points);
    }

    [Fact]
    public void Score_SeparatesGendersAndDivision()
    {
        var scored = ChampionshipScorer.Score(
            [
                Runner(Gender.Male, 1, division: Division.Seniori),
                Runner(Gender.Female, 1, division: Division.Seniori),
                Runner(Gender.Male, 1, division: Division.Juniori),
            ],
            Division.Seniori);

        Assert.Equal(2, scored.Count);
        Assert.Equal(100, scored.Single(s => s.Gender == Gender.Male).Points);
        Assert.Equal(100, scored.Single(s => s.Gender == Gender.Female).Points);
    }

    [Fact]
    public void Score_BeyondLadder_ScoresZero()
    {
        // 32 eligible men; the current ladder ends at 31, so rank 32 scores 0.
        var runners = Enumerable.Range(1, 32).Select(p => Runner(Gender.Male, p, name: $"M{p}")).ToArray();

        var scored = ChampionshipScorer.Score(runners, Division.Seniori);

        Assert.Equal(1, scored[30].Points);  // rank 31
        Assert.Equal(0, scored[31].Points);  // rank 32
    }
}
