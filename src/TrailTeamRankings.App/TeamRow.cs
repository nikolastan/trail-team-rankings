using TrailTeamRankings.Core.Ranking;

namespace TrailTeamRankings.App;

/// <summary>
/// Flattened team standing for the DataGrid: the three counting slots as display
/// strings (points, or "—" when a slot is empty) plus the total.
/// </summary>
public sealed record TeamRow(
    int Rank,
    string Club,
    string Male1,
    string Male2,
    string Female,
    int Total,
    bool Complete)
{
    public static TeamRow From(TeamStanding standing) => new(
        standing.Rank,
        standing.Club,
        Slot(standing.CountingMales, 0),
        Slot(standing.CountingMales, 1),
        standing.CountingFemale?.Points.ToString() ?? "—",
        standing.TotalPoints,
        standing.IsComplete);

    private static string Slot(IReadOnlyList<CountingRunner> males, int index) =>
        index < males.Count ? males[index].Points.ToString() : "—";
}
