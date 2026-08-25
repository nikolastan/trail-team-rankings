using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Core.Racing;

/// <summary>
/// Outcome of scraping a race results page: the raw scraped runners plus
/// metadata (race title, when it was scraped), or a failure carrying
/// human-readable messages. Mirrors the shape of <c>RegistryReadResult</c>.
/// </summary>
public sealed class RaceScrapeResult
{
    private RaceScrapeResult(
        bool isValid,
        IReadOnlyList<ScrapedRunner> runners,
        IReadOnlyList<string> errors,
        string? raceTitle,
        DateTimeOffset scrapedAt)
    {
        IsValid = isValid;
        Runners = runners;
        Errors = errors;
        RaceTitle = raceTitle;
        ScrapedAt = scrapedAt;
    }

    /// <summary>True when the page parsed into at least one runner.</summary>
    public bool IsValid { get; }

    /// <summary>Raw scraped runners; empty when <see cref="IsValid"/> is false.</summary>
    public IReadOnlyList<ScrapedRunner> Runners { get; }

    /// <summary>Messages explaining a fetch or parse failure.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>Page title, used as a race label in the UI when available.</summary>
    public string? RaceTitle { get; }

    /// <summary>When the scrape completed (for the "last updated" indicator).</summary>
    public DateTimeOffset ScrapedAt { get; }

    public static RaceScrapeResult Success(
        IReadOnlyList<ScrapedRunner> runners, string? raceTitle = null) =>
        new(isValid: true, runners, [], raceTitle, DateTimeOffset.Now);

    public static RaceScrapeResult Failure(params string[] errors) =>
        new(isValid: false, [], errors, null, DateTimeOffset.Now);
}
