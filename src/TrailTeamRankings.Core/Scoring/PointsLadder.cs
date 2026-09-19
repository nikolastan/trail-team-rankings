namespace TrailTeamRankings.Core.Scoring;

/// <summary>
/// Maps a finishing place to championship points using the fixed ladder taken
/// from the reference results file. Places beyond the ladder earn a single
/// participation point (per the mentor's results), not zero — every counted
/// finisher scores at least 1.
/// </summary>
public static class PointsLadder
{
    private const int ParticipationPoints = 1;

    private static readonly int[] PointsByPlace =
    [
        100, 88, 78, 70, 64, 60, 56, 52, 48, 44,
        40, 38, 36, 34, 32, 30, 28, 26, 24, 22,
        20, 18, 16, 14, 12, 10, 8, 6, 4, 2,
        1,
    ];

    /// <summary>
    /// The last place with an explicit ladder value (31). Beyond it every finisher
    /// earns the participation point.
    /// </summary>
    public static int LastScoringPlace => PointsByPlace.Length;

    /// <summary>
    /// Returns the points awarded for a given finishing place.
    /// </summary>
    /// <param name="place">1-based finishing place.</param>
    /// <returns>The ladder value for the place, or 1 (participation) beyond the ladder.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="place"/> is less than 1.</exception>
    public static int GetPoints(int place)
    {
        if (place < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(place), place, "Finishing place must be 1 or greater.");
        }

        return place <= PointsByPlace.Length ? PointsByPlace[place - 1] : ParticipationPoints;
    }
}
