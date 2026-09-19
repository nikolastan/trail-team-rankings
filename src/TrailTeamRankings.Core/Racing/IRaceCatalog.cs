namespace TrailTeamRankings.Core.Racing;

/// <summary>
/// Lists the races the user can pick from, so the UI can offer a selector instead
/// of a raw results URL. Implementations live in the Infrastructure layer; Core
/// depends only on this abstraction, leaving room for providers beyond RunTrace.
/// </summary>
public interface IRaceCatalog
{
    /// <summary>
    /// Fetches the available races. Does not throw for network or parse problems —
    /// inspect <see cref="RaceCatalogResult.IsValid"/>. Cancellation surfaces as
    /// <see cref="OperationCanceledException"/>.
    /// </summary>
    Task<RaceCatalogResult> GetRacesAsync(CancellationToken cancellationToken = default);
}
