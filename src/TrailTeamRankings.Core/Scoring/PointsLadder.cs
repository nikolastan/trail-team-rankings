namespace TrailTeamRankings.Core.Scoring;

/// <summary>
/// Maps a finishing place to championship points using the fixed ladder taken
/// from the reference results file. Places beyond the ladder score zero.
/// </summary>
public static class PointsLadder
{
    private static readonly int[] PointsByPlace =
    [
        100, 88, 78, 70, 64, 60, 56, 52, 48, 44,
        40, 38, 36, 34, 32, 30, 28, 26, 24, 22,
        20, 18, 16, 14, 12, 10, 8, 6, 4, 2,
        1,
    ];

    /// <summary>
    /// The lowest place that still earns points. Places greater than this score zero.
    /// </summary>
    public static int LastScoringPlace => PointsByPlace.Length;

    /// <summary>
    /// Returns the points awarded for a given finishing place.
    /// </summary>
    /// <param name="place">1-based finishing place.</param>
    /// <returns>Points for the place, or 0 if the place is beyond the ladder.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="place"/> is less than 1.</exception>
    public static int GetPoints(int place)
    {
        if (place < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(place), place, "Finishing place must be 1 or greater.");
        }

        return place <= PointsByPlace.Length ? PointsByPlace[place - 1] : 0;
    }
}
