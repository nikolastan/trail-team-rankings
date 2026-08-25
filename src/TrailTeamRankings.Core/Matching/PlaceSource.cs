namespace TrailTeamRankings.Core.Matching;

/// <summary>
/// Which RunTrace place column awards points. The plan's open question (§12 #1)
/// leaves this to the pipeline; the scraper captures both, so this is a one-line
/// switch. Defaults to <see cref="CategoryPlace"/> per the current assumption.
/// </summary>
public enum PlaceSource
{
    CategoryPlace,
    OverallPlace,
}
