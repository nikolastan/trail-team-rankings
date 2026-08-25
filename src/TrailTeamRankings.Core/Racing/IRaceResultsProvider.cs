namespace TrailTeamRankings.Core.Racing;

/// <summary>
/// Fetches and parses race results from a results URL. Implementations live in
/// the Infrastructure layer; Core depends only on this abstraction, which keeps
/// the door open for additional providers beyond RunTrace.
/// </summary>
public interface IRaceResultsProvider
{
    /// <summary>
    /// Fetches and parses results from the given URL. Does not throw for network
    /// or parse problems — inspect <see cref="RaceScrapeResult.IsValid"/>.
    /// Cancellation (e.g. live-poll shutdown) surfaces as
    /// <see cref="OperationCanceledException"/>.
    /// </summary>
    Task<RaceScrapeResult> GetResultsAsync(string url, CancellationToken cancellationToken = default);
}
