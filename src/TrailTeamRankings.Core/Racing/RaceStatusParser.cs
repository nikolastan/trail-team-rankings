using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Core.Racing;

/// <summary>
/// Maps a RunTrace status label ("Finished", "DNF", "OOR", …) to a
/// <see cref="RaceStatus"/>. Unrecognized or blank values map to
/// <see cref="RaceStatus.Unknown"/>, so an unexpected status can never be
/// mistaken for a finisher.
/// </summary>
public static class RaceStatusParser
{
    private static readonly Dictionary<string, RaceStatus> ByLabel =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Finished"] = RaceStatus.Finished,
            ["Racing"] = RaceStatus.Racing,
            ["Ready"] = RaceStatus.Ready,
            ["OOR"] = RaceStatus.Oor,
            ["DNF"] = RaceStatus.Dnf,
            ["DISQ"] = RaceStatus.Disq,
            ["DNS"] = RaceStatus.Dns,
            ["Late"] = RaceStatus.Late,
            ["Registered"] = RaceStatus.Registered,
            ["Pending"] = RaceStatus.Pending,
            ["Rejected"] = RaceStatus.Rejected,
        };

    public static RaceStatus Parse(string? statusText) =>
        !string.IsNullOrWhiteSpace(statusText) && ByLabel.TryGetValue(statusText.Trim(), out var status)
            ? status
            : RaceStatus.Unknown;
}
