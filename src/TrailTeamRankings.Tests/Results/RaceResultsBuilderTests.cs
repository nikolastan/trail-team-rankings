using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Results;

namespace TrailTeamRankings.Tests.Results;

public class RaceResultsBuilderTests
{
    private static readonly DateOnly RaceDate = new(2026, 9, 1);

    private static RegisteredAthlete Athlete(string name) =>
        new(name, "Klub", null, null, new MedicalClearance("важи до 01.11.2026", new DateOnly(2026, 11, 1)));

    private static ScrapedRunner Scraped(
        string name,
        string category,
        string club,
        int place,
        string status = "Finished") =>
        new(name, "1", category, club, "1:00:00", status, place, place);

    // A field with a complete Seniori team (Club A), an incomplete one (Club B),
    // a junior, a non-finisher, and an unmappable category.
    private static RaceResults BuildSample() =>
        RaceResultsBuilder.Build(
            registry:
            [
                Athlete("Ana A"), Athlete("Marko A"), Athlete("Petar A"),
                Athlete("Boban B"), Athlete("Junior J"), Athlete("Dns D"),
            ],
            scrapedRunners:
            [
                Scraped("Marko A", "Apsolutna M", "Club A", 1),   // 100
                Scraped("Petar A", "Apsolutna M", "Club A", 2),   // 88
                Scraped("Ana A", "Apsolutna Ž", "Club A", 1),     // 100
                Scraped("Boban B", "Apsolutna M", "Club B", 3),   // 78 (incomplete team)
                Scraped("Junior J", "Juniori", "Club A", 1),      // junior division
                Scraped("Dns D", "Apsolutna M", "Club A", 0, status: "DNS"),
                Scraped("Neko N", "Rekreativci", "Club C", 1),    // unmappable category
            ],
            raceDate: RaceDate,
            raceTitle: "Test Race");

    [Fact]
    public void Build_SetsMetadata()
    {
        var results = BuildSample();

        Assert.Equal("Test Race", results.RaceTitle);
        Assert.Equal(RaceDate, results.RaceDate);
    }

    [Fact]
    public void Build_RanksCompleteSenioriTeamFirst()
    {
        var seniori = BuildSample().Seniori;

        var top = seniori.TeamStandings[0];
        Assert.Equal("Club A", top.Club);
        Assert.Equal(1, top.Rank);
        Assert.True(top.IsComplete);
        Assert.Equal(100 + 88 + 100, top.TotalPoints);
    }

    [Fact]
    public void Build_SplitsIndividualsByGender_AndRanksByPoints()
    {
        var seniori = BuildSample().Seniori;

        // Males: Marko(100), Petar(88), Boban(78) — DNS runner is not eligible-finished.
        Assert.Equal(["Marko A", "Petar A", "Boban B"], seniori.MaleIndividuals.Select(i => i.Name));
        Assert.Equal([1, 2, 3], seniori.MaleIndividuals.Select(i => i.Rank));
        Assert.Equal(100, seniori.MaleIndividuals[0].Points);

        Assert.Equal("Ana A", Assert.Single(seniori.FemaleIndividuals).Name);
    }

    [Fact]
    public void Build_KeepsDivisionsSeparate()
    {
        var results = BuildSample();

        Assert.Single(results.Juniori.MaleIndividuals);
        Assert.Equal("Junior J", results.Juniori.MaleIndividuals[0].Name);
        Assert.DoesNotContain(results.Seniori.MaleIndividuals, i => i.Name == "Junior J");
    }

    [Fact]
    public void Build_PutsNonFinisherInDivisionExcluded()
    {
        var seniori = BuildSample().Seniori;

        Assert.Contains(seniori.ExcludedRunners,
            e => e.Name == "Dns D" && e.Reason == ExclusionReason.NotFinished);
    }

    [Fact]
    public void Build_PutsUnmappableCategoryInUnclassifiedExcluded()
    {
        var results = BuildSample();

        var excluded = Assert.Single(results.UnclassifiedExcluded);
        Assert.Equal("Neko N", excluded.Name);
        Assert.Equal(ExclusionReason.UnknownCategory, excluded.Reason);
        Assert.DoesNotContain(results.AllRunners, r => r.Name == "Neko N");
    }
}
