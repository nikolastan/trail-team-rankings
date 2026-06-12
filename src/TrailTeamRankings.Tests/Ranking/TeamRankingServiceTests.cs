using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Ranking;
using TrailTeamRankings.Core.Scoring;

namespace TrailTeamRankings.Tests.Ranking;

public class TeamRankingServiceTests
{
    private readonly TeamRankingService _service = new();

    private static RaceRunner Runner(
        string club,
        Gender gender,
        int place,
        string? name = null,
        Division division = Division.Seniori,
        RaceStatus status = RaceStatus.Finished,
        bool eligible = true) =>
        new(
            name ?? $"{club}-{gender}-{place}",
            club,
            gender,
            division,
            place,
            status,
            eligible);

    [Fact]
    public void RankTeams_PicksBestTwoMalesAndBestFemale()
    {
        var runners = new[]
        {
            Runner("Club A", Gender.Male, 1),   // 100
            Runner("Club A", Gender.Male, 2),   // 88
            Runner("Club A", Gender.Male, 5),   // 64 (dropped)
            Runner("Club A", Gender.Female, 1), // 100
            Runner("Club A", Gender.Female, 3), // 78 (dropped)
        };

        var standing = Assert.Single(_service.RankTeams(runners, Division.Seniori));

        Assert.True(standing.IsComplete);
        Assert.Equal(100 + 88 + 100, standing.TotalPoints);
        Assert.Equal([100, 88], standing.CountingMales.Select(m => m.Points));
        Assert.Equal(100, standing.CountingFemale!.Points);
    }

    [Fact]
    public void RankTeams_IncompleteTeam_CountsAvailableSlotsAsPartialTotal()
    {
        var runners = new[]
        {
            Runner("Solo", Gender.Male, 1), // 100, no second male, no female
        };

        var standing = Assert.Single(_service.RankTeams(runners, Division.Seniori));

        Assert.False(standing.IsComplete);
        Assert.Equal(100, standing.TotalPoints);
        Assert.Single(standing.CountingMales);
        Assert.Null(standing.CountingFemale);
    }

    [Fact]
    public void RankTeams_ExcludesNonFinishers()
    {
        var runners = new[]
        {
            Runner("Club A", Gender.Male, 1, status: RaceStatus.Dnf),
            Runner("Club A", Gender.Male, 2, status: RaceStatus.Finished),
            Runner("Club A", Gender.Female, 1, status: RaceStatus.Dns),
        };

        var standing = Assert.Single(_service.RankTeams(runners, Division.Seniori));

        Assert.Equal(88, standing.TotalPoints);
        Assert.Single(standing.CountingMales);
        Assert.Null(standing.CountingFemale);
    }

    [Fact]
    public void RankTeams_ExcludesIneligibleRunners()
    {
        var runners = new[]
        {
            Runner("Club A", Gender.Male, 1, eligible: false),
            Runner("Club A", Gender.Male, 2, eligible: true),
        };

        var standing = Assert.Single(_service.RankTeams(runners, Division.Seniori));

        Assert.Equal(88, standing.TotalPoints);
        Assert.Single(standing.CountingMales);
    }

    [Fact]
    public void RankTeams_OnlyIncludesRequestedDivision()
    {
        var runners = new[]
        {
            Runner("Club A", Gender.Male, 1, division: Division.Seniori),
            Runner("Club A", Gender.Male, 1, division: Division.Juniori),
        };

        var seniori = Assert.Single(_service.RankTeams(runners, Division.Seniori));
        var juniori = Assert.Single(_service.RankTeams(runners, Division.Juniori));

        Assert.Equal(100, seniori.TotalPoints);
        Assert.Equal(100, juniori.TotalPoints);
        Assert.Equal(Division.Seniori, seniori.Division);
        Assert.Equal(Division.Juniori, juniori.Division);
    }

    [Fact]
    public void RankTeams_OrdersByTotalDescending_AndAssignsRanks()
    {
        var runners = new[]
        {
            // Weak team: 64 + 60 + 56 = 180
            Runner("Weak", Gender.Male, 5),
            Runner("Weak", Gender.Male, 6),
            Runner("Weak", Gender.Female, 7),
            // Strong team: 100 + 88 + 78 = 266
            Runner("Strong", Gender.Male, 1),
            Runner("Strong", Gender.Male, 2),
            Runner("Strong", Gender.Female, 3),
        };

        var standings = _service.RankTeams(runners, Division.Seniori);

        Assert.Equal("Strong", standings[0].Club);
        Assert.Equal(1, standings[0].Rank);
        Assert.Equal("Weak", standings[1].Club);
        Assert.Equal(2, standings[1].Rank);
    }

    [Fact]
    public void RankTeams_TieOnTotal_PrefersCompleteTeam()
    {
        var runners = new[]
        {
            // Incomplete team: single male at place 1 = 100
            Runner("Incomplete", Gender.Male, 1),
            // Complete team also totaling 100: 64 (p5) + 32 (p15) + 4 (p29)
            Runner("Complete", Gender.Male, 5),
            Runner("Complete", Gender.Male, 15),
            Runner("Complete", Gender.Female, 29),
        };

        var standings = _service.RankTeams(runners, Division.Seniori);

        Assert.Equal(standings[0].TotalPoints, standings[1].TotalPoints);
        Assert.Equal("Complete", standings[0].Club);
        Assert.True(standings[0].IsComplete);
        Assert.False(standings[1].IsComplete);
    }

    [Fact]
    public void RankTeams_GroupsClubCaseInsensitively()
    {
        var runners = new[]
        {
            Runner("PSD Ćira", Gender.Male, 1),
            Runner("psd ćira", Gender.Male, 2),
        };

        var standing = Assert.Single(_service.RankTeams(runners, Division.Seniori));

        Assert.Equal(100 + 88, standing.TotalPoints);
        Assert.Equal(2, standing.CountingMales.Count);
    }

    [Fact]
    public void RankTeams_BeyondLadder_ContributesZero()
    {
        var runners = new[]
        {
            Runner("Club A", Gender.Male, 1),  // 100
            Runner("Club A", Gender.Male, 50), // 0
            Runner("Club A", Gender.Female, 40), // 0
        };

        var standing = Assert.Single(_service.RankTeams(runners, Division.Seniori));

        Assert.Equal(100, standing.TotalPoints);
        Assert.True(standing.IsComplete);
        Assert.Equal(PointsLadder.GetPoints(50), standing.CountingMales[1].Points);
    }

    [Fact]
    public void RankTeams_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(_service.RankTeams([], Division.Seniori));
    }
}
