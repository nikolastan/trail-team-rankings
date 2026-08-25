using TrailTeamRankings.Core.Matching;
using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Tests.Matching;

public class RaceFieldResolverTests
{
    private static readonly DateOnly RaceDate = new(2026, 9, 1);

    private static RegisteredAthlete Athlete(string name, string club, MedicalClearance medical) =>
        new(name, club, null, null, medical);

    private static MedicalClearance ValidUntil(DateOnly date) =>
        new($"важи до {date:dd.MM.yyyy}", date);

    private static ScrapedRunner Scraped(
        string name,
        string category = "Apsolutna M",
        string? club = "Klub A",
        string status = "Finished",
        int? overall = 1,
        int? categoryPlace = 1) =>
        new(name, "1", category, club, "1:00:00", status, overall, categoryPlace);

    [Fact]
    public void Resolve_EligibleFinisher_IsRankedAndNotExcluded()
    {
        var registry = new[] { Athlete("Ђуро Борбељ", "ПК Железничар", ValidUntil(new(2026, 11, 1))) };

        var field = new RaceFieldResolver(registry)
            .Resolve([Scraped("Đuro Borbelj", club: "PK Železničar Inđija")], RaceDate);

        var runner = Assert.Single(field.Runners);
        Assert.True(runner.IsEligible);
        Assert.Equal(Division.Seniori, runner.Division);
        Assert.Equal(Gender.Male, runner.Gender);
        Assert.Empty(field.Excluded);
    }

    [Fact]
    public void Resolve_ExpiredMedical_ExcludesAndMarksIneligible()
    {
        var registry = new[] { Athlete("Марко Марковић", "Клуб", ValidUntil(new(2026, 7, 1))) };

        var field = new RaceFieldResolver(registry).Resolve([Scraped("Marko Marković")], RaceDate);

        Assert.False(Assert.Single(field.Runners).IsEligible);
        Assert.Equal(ExclusionReason.MedicalExpired, Assert.Single(field.Excluded).Reason);
    }

    [Fact]
    public void Resolve_NoMedicalData_Excludes()
    {
        var registry = new[] { Athlete("Марко Марковић", "Клуб", MedicalClearance.None) };

        var field = new RaceFieldResolver(registry).Resolve([Scraped("Marko Marković")], RaceDate);

        Assert.Equal(ExclusionReason.NoMedicalData, Assert.Single(field.Excluded).Reason);
    }

    [Fact]
    public void Resolve_NotInRegistry_Excludes()
    {
        var field = new RaceFieldResolver([]).Resolve([Scraped("Nobody Special")], RaceDate);

        Assert.False(Assert.Single(field.Runners).IsEligible);
        Assert.Equal(ExclusionReason.NotInRegistry, Assert.Single(field.Excluded).Reason);
    }

    [Fact]
    public void Resolve_NonFinisher_IsExcludedAsNotFinished()
    {
        var registry = new[] { Athlete("Ана Анић", "Клуб", ValidUntil(new(2026, 11, 1))) };

        var field = new RaceFieldResolver(registry).Resolve(
            [Scraped("Ana Anić", category: "Apsolutna Ž", status: "DNS", overall: null, categoryPlace: null)],
            RaceDate);

        Assert.Equal(ExclusionReason.NotFinished, Assert.Single(field.Excluded).Reason);
        var runner = Assert.Single(field.Runners);
        Assert.Equal(RaceStatus.Dns, runner.Status);
        Assert.Equal(Gender.Female, runner.Gender);
    }

    [Fact]
    public void Resolve_UnknownCategory_IsExcluded_AndNotRanked()
    {
        var field = new RaceFieldResolver([]).Resolve([Scraped("Neko Neznani", category: "Rekreativci")], RaceDate);

        Assert.Empty(field.Runners);
        Assert.Equal(ExclusionReason.UnknownCategory, Assert.Single(field.Excluded).Reason);
    }

    [Fact]
    public void Resolve_UsesCategoryPlaceByDefault_AndOverallWhenConfigured()
    {
        var registry = new[] { Athlete("Ана Анић", "Клуб", ValidUntil(new(2026, 11, 1))) };
        ScrapedRunner[] scraped = [Scraped("Ana Anić", category: "Apsolutna Ž", overall: 5, categoryPlace: 2)];

        var byCategory = new RaceFieldResolver(registry).Resolve(scraped, RaceDate);
        Assert.Equal(2, Assert.Single(byCategory.Runners).Place);

        var byOverall = new RaceFieldResolver(registry, PlaceSource.OverallPlace).Resolve(scraped, RaceDate);
        Assert.Equal(5, Assert.Single(byOverall.Runners).Place);
    }

    [Fact]
    public void Resolve_JuniorCategory_MapsToJunioriDivision()
    {
        var field = new RaceFieldResolver([]).Resolve([Scraped("Neki Junior", category: "Juniori")], RaceDate);

        Assert.Equal(Division.Juniori, Assert.Single(field.Runners).Division);
    }
}
