using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Core.Ranking;

/// <summary>
/// The team result for a single club within a division: the counting runners
/// (top males and females) and their summed points.
/// </summary>
public sealed class TeamStanding
{
    public required Division Division { get; init; }

    public required string Club { get; init; }

    /// <summary>Counting males, ordered best (highest points) first.</summary>
    public required IReadOnlyList<CountingRunner> CountingMales { get; init; }

    /// <summary>Counting female, or null when the club had no eligible female finisher.</summary>
    public CountingRunner? CountingFemale { get; init; }

    /// <summary>True when the club filled all counting slots for both genders.</summary>
    public required bool IsComplete { get; init; }

    /// <summary>1-based standing position within the division.</summary>
    public int Rank { get; set; }

    public int TotalPoints =>
        CountingMales.Sum(runner => runner.Points) + (CountingFemale?.Points ?? 0);
}
