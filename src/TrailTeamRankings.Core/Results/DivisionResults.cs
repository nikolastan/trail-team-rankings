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
}
