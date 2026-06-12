using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Core.Ranking;

/// <summary>
/// A runner that contributes points to a team standing, with the points already
/// resolved from their finishing place.
/// </summary>
public sealed record CountingRunner(
    string Name,
    string Club,
    Gender Gender,
    int Place,
    int Points);
