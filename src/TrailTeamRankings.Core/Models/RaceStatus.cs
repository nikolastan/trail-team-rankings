namespace TrailTeamRankings.Core.Models;

/// <summary>
/// Result status as published by RunTrace. Only <see cref="Finished"/> runners
/// count toward team standings.
/// </summary>
public enum RaceStatus
{
    Unknown,
    Finished,
    Racing,
    Ready,
    Oor,
    Dnf,
    Disq,
    Dns,
    Late,
}
