using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Results;
using TrailTeamRankings.Infrastructure.Export;

namespace TrailTeamRankings.Tests.Export;

public class PdfResultsExporterTests
{
    private static RegisteredAthlete Athlete(string name) =>
        new(name, "Klub", null, null, new MedicalClearance("важи до 01.11.2026", new DateOnly(2026, 11, 1)));

    private static ScrapedRunner Scraped(string name, string category, string club, int place) =>
        new(name, "1", category, club, "1:00:00", "Finished", place, place);

    private static RaceResults SampleResults() =>
        RaceResultsBuilder.Build(
            registry: [Athlete("Marko A"), Athlete("Petar A"), Athlete("Ana A")],
            scrapedRunners:
            [
                Scraped("Marko A", "Apsolutna M", "Club A", 1),
                Scraped("Petar A", "Apsolutna M", "Club A", 2),
                Scraped("Ana A", "Apsolutna Ž", "Club A", 1),
            ],
            raceDate: new DateOnly(2026, 9, 1),
            raceTitle: "Test Race");

    [Fact]
    public void FileExtension_IsPdf()
    {
        Assert.Equal("pdf", new PdfResultsExporter().FileExtension);
    }

    [Fact]
    public void Export_ProducesPdfBytes()
    {
        using var stream = new MemoryStream();
        new PdfResultsExporter().Export(SampleResults(), stream);

        var bytes = stream.ToArray();
        Assert.True(bytes.Length > 1000, "PDF should be non-trivial in size.");
        Assert.Equal("%PDF"u8.ToArray(), bytes[..4]); // PDF magic header
    }

    [Fact]
    public void Export_DoesNotThrow_WhenDivisionsEmpty()
    {
        var results = RaceResultsBuilder.Build([], [], new DateOnly(2026, 9, 1));
        using var stream = new MemoryStream();

        new PdfResultsExporter().Export(results, stream);

        Assert.True(stream.Length > 0);
    }
}
