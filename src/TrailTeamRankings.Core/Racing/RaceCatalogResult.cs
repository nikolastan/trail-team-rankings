namespace TrailTeamRankings.Core.Racing;

/// <summary>
/// Outcome of listing the available races: the parsed listings, or a failure
/// carrying human-readable messages. Mirrors <see cref="RaceScrapeResult"/>.
/// </summary>
public sealed class RaceCatalogResult
{
    private RaceCatalogResult(bool isValid, IReadOnlyList<RaceListing> races, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Races = races;
        Errors = errors;
    }

    /// <summary>True when the catalog was fetched and parsed successfully.</summary>
    public bool IsValid { get; }

    /// <summary>The races found; empty when <see cref="IsValid"/> is false.</summary>
    public IReadOnlyList<RaceListing> Races { get; }

    /// <summary>Messages explaining a fetch or parse failure.</summary>
    public IReadOnlyList<string> Errors { get; }

    public static RaceCatalogResult Success(IReadOnlyList<RaceListing> races) =>
        new(isValid: true, races, []);

    public static RaceCatalogResult Failure(params string[] errors) =>
        new(isValid: false, [], errors);
}
