using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Core.Results;

/// <summary>
/// The single canonical result object produced by the pipeline and consumed by
/// the UI and every exporter. Holds both divisions plus the raw resolved field
/// and metadata.
/// </summary>
public sealed class RaceResults
{
    public required DivisionResults Seniori { get; init; }

    public required DivisionResults Juniori { get; init; }

    /// <summary>Every mappable runner (all divisions), for the "All runners" view.</summary>
    public required IReadOnlyList<RaceRunner> AllRunners { get; init; }

    /// <summary>Excluded runners whose category did not map to a division.</summary>
    public required IReadOnlyList<ExcludedRunner> UnclassifiedExcluded { get; init; }

    public string? RaceTitle { get; init; }

    public DateOnly RaceDate { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }

    /// <summary>Returns the results for the given division.</summary>
    public DivisionResults For(Division division) => division switch
    {
        Division.Seniori => Seniori,
        Division.Juniori => Juniori,
        _ => throw new ArgumentOutOfRangeException(nameof(division)),
    };
}
