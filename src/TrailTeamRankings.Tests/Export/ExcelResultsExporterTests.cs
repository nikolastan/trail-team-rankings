using ClosedXML.Excel;
using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Results;
using TrailTeamRankings.Infrastructure.Export;

namespace TrailTeamRankings.Tests.Export;

public class ExcelResultsExporterTests
{
    private static RegisteredAthlete Athlete(string name) =>
        new(name, "Klub", null, null, new MedicalClearance("важи до 01.11.2026", new DateOnly(2026, 11, 1)));

    private static ScrapedRunner Scraped(string name, string category, string club, int place) =>
        new(name, "1", category, club, "1:00:00", "Finished", place, place);

    private static RaceResults SampleResults() =>
        RaceResultsBuilder.Build(
            registry: [Athlete("Marko A"), Athlete("Petar A"), Athlete("Ana A"), Athlete("Junior J")],
            scrapedRunners:
            [
                Scraped("Marko A", "Apsolutna M", "Club A", 1), // 100
                Scraped("Petar A", "Apsolutna M", "Club A", 2), // 88
                Scraped("Ana A", "Apsolutna Ž", "Club A", 1),   // 100
                Scraped("Junior J", "Juniori", "Club J", 1),    // junior male
            ],
            raceDate: new DateOnly(2026, 9, 1));

    private static XLWorkbook Export(RaceResults results)
    {
        var stream = new MemoryStream();
        new ExcelResultsExporter().Export(results, stream);
        stream.Position = 0;
        return new XLWorkbook(stream);
    }

    [Fact]
    public void Export_CreatesBothDivisionSheets()
    {
        using var workbook = Export(SampleResults());

        Assert.True(workbook.TryGetWorksheet("Seniori", out _));
        Assert.True(workbook.TryGetWorksheet("Juniori", out _));
    }

    [Fact]
    public void Export_WritesBlockTitlesAndHeaders()
    {
        using var workbook = Export(SampleResults());
        var sheet = workbook.Worksheet("Seniori");

        Assert.Equal("MUŠKARCI", sheet.Cell("A2").GetString());
        Assert.Equal("ŽENE", sheet.Cell("G2").GetString());
        Assert.Equal("EKIPNO", sheet.Cell("M2").GetString());

        Assert.Equal("plasman", sheet.Cell("A3").GetString());
        Assert.Equal("bodovi", sheet.Cell("D3").GetString());
        Assert.Equal("Klub", sheet.Cell("M3").GetString());
        Assert.Equal("muš 1", sheet.Cell("N3").GetString());
        Assert.Equal("žena 1", sheet.Cell("P3").GetString());
        Assert.Equal("ukupno", sheet.Cell("Q3").GetString());
    }

    [Fact]
    public void Export_WritesMenAndWomenIndividuals()
    {
        using var workbook = Export(SampleResults());
        var sheet = workbook.Worksheet("Seniori");

        // Men: Marko (100) first, Petar (88) second.
        Assert.Equal(1, sheet.Cell("A4").GetValue<int>());
        Assert.Equal("Marko A", sheet.Cell("B4").GetString());
        Assert.Equal(100, sheet.Cell("D4").GetValue<int>());
        Assert.Equal("Petar A", sheet.Cell("B5").GetString());

        // Women: Ana (100).
        Assert.Equal("Ana A", sheet.Cell("H4").GetString());
        Assert.Equal(100, sheet.Cell("J4").GetValue<int>());
    }

    [Fact]
    public void Export_WritesTeamBlock_WithRankClubSlotsAndTotal()
    {
        using var workbook = Export(SampleResults());
        var sheet = workbook.Worksheet("Seniori");

        Assert.Equal(1, sheet.Cell("L4").GetValue<int>());     // team rank
        Assert.Equal("Club A", sheet.Cell("M4").GetString());  // club
        Assert.Equal(100, sheet.Cell("N4").GetValue<int>());   // muš 1
        Assert.Equal(88, sheet.Cell("O4").GetValue<int>());    // muš 2
        Assert.Equal(100, sheet.Cell("P4").GetValue<int>());   // žena 1
        Assert.Equal(288, sheet.Cell("Q4").GetValue<int>());   // ukupno
    }

    [Fact]
    public void Export_AddsTeamBlockToJuniori_EvenThoughSampleLacksIt()
    {
        using var workbook = Export(SampleResults());
        var juniori = workbook.Worksheet("Juniori");

        Assert.Equal("EKIPNO", juniori.Cell("M2").GetString());
        Assert.Equal("ukupno", juniori.Cell("Q3").GetString());
        // The junior male's club forms an (incomplete) team.
        Assert.Equal("Club J", juniori.Cell("M4").GetString());
    }

    [Fact]
    public void Export_LeavesMissingTeamSlotsBlank()
    {
        using var workbook = Export(SampleResults());
        var juniori = workbook.Worksheet("Juniori");

        // Junior team has one male, no second male and no female → those cells empty.
        Assert.True(juniori.Cell("O4").IsEmpty()); // muš 2
        Assert.True(juniori.Cell("P4").IsEmpty()); // žena 1
    }
}
