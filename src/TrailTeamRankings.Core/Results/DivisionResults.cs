using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Ranking;

namespace TrailTeamRankings.Core.Results;

/// <summary>
/// The complete results for one division: team standings, individual standings
/// (men and women separately), and the runners excluded from team scoring.
/// </summary>
public sealed class DivisionResults
{
    public required Division Division { get; init; }

    public required IReadOnlyList<TeamStanding> TeamStandings { get; init; }

    public required IReadOnlyList<IndividualResult> MaleIndividuals { get; init; }

    public required IReadOnlyList<IndividualResult> FemaleIndividuals { get; init; }

    public required IReadOnlyList<ExcludedRunner> ExcludedRunners { get; init; }

    /// <summary>
    /// True when any runner at all — ranked, individual, or excluded — competed in
    /// this division. Used to omit divisions no one entered (e.g. Juniori in a
    /// seniors-only race) from exports and to flag empty tabs in the UI.
    /// </summary>
    public bool HasRunners =>
        TeamStandings.Count > 0 ||
        MaleIndividuals.Count > 0 ||
        FemaleIndividuals.Count > 0 ||
        ExcludedRunners.Count > 0;
}
