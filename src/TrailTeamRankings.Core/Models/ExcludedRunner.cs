namespace TrailTeamRankings.Core.Models;

/// <summary>
/// A runner that was left out of team scoring, tagged with the reason. Populated
/// during matching and surfaced in the Excluded panel.
/// </summary>
/// <param name="Name">Runner name as scraped from RunTrace.</param>
/// <param name="Club">Club / team, when present.</param>
/// <param name="CategoryLabel">Original RunTrace category text.</param>
/// <param name="Division">Resolved division, or null when the category was unknown.</param>
/// <param name="Status">Parsed result status.</param>
/// <param name="Reason">Why the runner was excluded.</param>
public sealed record ExcludedRunner(
    string Name,
    string? Club,
    string CategoryLabel,
    Division? Division,
    RaceStatus Status,
    ExclusionReason Reason);
