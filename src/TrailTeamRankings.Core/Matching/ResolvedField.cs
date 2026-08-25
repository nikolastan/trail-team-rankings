using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Core.Matching;

/// <summary>
/// The outcome of resolving a scraped field against the registry: every mappable
/// runner as a rank-ready <see cref="RaceRunner"/>, plus the runners that do not
/// count toward teams, each tagged with a reason.
/// </summary>
public sealed class ResolvedField
{
    public ResolvedField(IReadOnlyList<RaceRunner> runners, IReadOnlyList<ExcludedRunner> excluded)
    {
        Runners = runners;
        Excluded = excluded;
    }

    /// <summary>All runners whose category mapped to a division/gender, with
    /// gender, division, place, status and eligibility resolved.</summary>
    public IReadOnlyList<RaceRunner> Runners { get; }

    /// <summary>Runners excluded from team scoring, with the reason.</summary>
    public IReadOnlyList<ExcludedRunner> Excluded { get; }
}
