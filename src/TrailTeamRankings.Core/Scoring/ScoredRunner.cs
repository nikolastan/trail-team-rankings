using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Core.Scoring;

/// <summary>
/// An eligible finisher with the championship points they earned. Points are
/// awarded by <see cref="Rank"/> — the runner's position among the eligible
/// finishers of the same division and gender — not by the raw category place, so
/// unregistered/ineligible runners ahead of them do not cost them points.
/// </summary>
public sealed record ScoredRunner(
    string Name,
    string Club,
    Gender Gender,
    int Rank,
    int Points);
