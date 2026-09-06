using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Ranking;
using TrailTeamRankings.Core.Scoring;

namespace TrailTeamRankings.Tests.Ranking;

public class TeamRankingServiceTests
{
    private readonly TeamRankingService _service = new();

    private static ScoredRunner Runner(string club, Gender gender, int points, int rank = 1, string? name = null) =>
        new(name ?? $"{club}-{gender}-{points}", club, gender, rank, points);

    [Fact]
    public void RankTeams_PicksBestTwoMalesAndBestFemale()
    {
        var runners = new[]
        {
            Runner("Club A", Gender.Male, 100),
            Runner("Club A", Gender.Male, 88),
            Runner("Club A", Gender.Male, 64),   // dropped
            Runner("Club A", Gender.Female, 100),
            Runner("Club A", Gender.Female, 78),  // dropped
        };

        var standing = Assert.Single(_service.RankTeams(runners, Division.Seniori));

        Assert.True(standing.IsComplete);
        Assert.Equal(100 + 88 + 100, standing.TotalPoints);
        Assert.Equal([100, 88], standing.CountingMales.Select(m => m.Points));
        Assert.Equal(100, standing.CountingFemale!.Points);
    }

    [Fact]
    public void RankTeams_IncompleteTeam_CountsAvailableSlots()
    {
        var standing = Assert.Single(_service.RankTeams([Runner("Solo", Gender.Male, 100)], Division.Seniori));

        Assert.False(standing.IsComplete);
        Assert.Equal(100, standing.TotalPoints);
        Assert.Single(standing.CountingMales);
        Assert.Null(standing.CountingFemale);
    }

    [Fact]
    public void RankTeams_OrdersByTotalDescending_AndAssignsRanks()
    {
        var runners = new[]
        {
            Runner("Weak", Gender.Male, 64), Runner("Weak", Gender.Male, 60), Runner("Weak", Gender.Female, 56),
            Runner("Strong", Gender.Male, 100), Runner("Strong", Gender.Male, 88), Runner("Strong", Gender.Female, 78),
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
            Runner("Incomplete", Gender.Male, 100),
            Runner("Complete", Gender.Male, 64), Runner("Complete", Gender.Male, 32), Runner("Complete", Gender.Female, 4),
        };

        var standings = _service.RankTeams(runners, Division.Seniori);

        Assert.Equal(standings[0].TotalPoints, standings[1].TotalPoints);
        Assert.Equal("Complete", standings[0].Club);
        Assert.True(standings[0].IsComplete);
    }

    [Fact]
    public void RankTeams_GroupsClubCaseInsensitively()
    {
        var runners = new[] { Runner("PSD Ćira", Gender.Male, 100), Runner("psd ćira", Gender.Male, 88) };

        var standing = Assert.Single(_service.RankTeams(runners, Division.Seniori));

        Assert.Equal(100 + 88, standing.TotalPoints);
        Assert.Equal(2, standing.CountingMales.Count);
    }

    [Fact]
    public void RankTeams_GroupsClubs_IgnoringDiacritics()
    {
        var runners = new[]
        {
            Runner("PK Tara Bajina Bašta", Gender.Male, 100),
            Runner("PK Tara Bajina Basta", Gender.Male, 88),
        };

        var standing = Assert.Single(_service.RankTeams(runners, Division.Seniori));

        Assert.Equal(100 + 88, standing.TotalPoints);
    }

    [Fact]
    public void RankTeams_ExcludesRunnersWithoutClub()
    {
        var runners = new[] { Runner("", Gender.Male, 100), Runner("   ", Gender.Male, 88) };

        Assert.Empty(_service.RankTeams(runners, Division.Seniori));
    }

    [Fact]
    public void RankTeams_DisplaysMostCommonClubSpelling()
    {
        var runners = new[]
        {
            Runner("PSK Balkan", Gender.Male, 100),
            Runner("PSK Balkan", Gender.Male, 88),
            Runner("psk balkan", Gender.Female, 100),
        };

        var standing = Assert.Single(_service.RankTeams(runners, Division.Seniori));

        Assert.Equal("PSK Balkan", standing.Club);
    }

    [Fact]
    public void RankTeams_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(_service.RankTeams([], Division.Seniori));
    }
}
