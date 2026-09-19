namespace TrailTeamRankings.Core.Racing;

/// <summary>Whether a race is still upcoming/running or already over.</summary>
public enum EventStatus
{
    /// <summary>Upcoming or in progress (RunTrace <c>active</c>).</summary>
    Active,

    /// <summary>Completed (RunTrace <c>passed</c>).</summary>
    Finished,
}

/// <summary>
/// One race in the provider's catalog: enough to show it in a picker and to build
/// its results URL. Provider-agnostic — RunTrace-specific parsing lives in the
/// Infrastructure layer.
/// </summary>
public sealed record RaceListing(
    string Slug,
    string Title,
    DateOnly? Date,
    string? Location,
    EventStatus Status,
    bool HasResults,
    string ResultsUrl);
