namespace TrailTeamRankings.Core.Registry;

/// <summary>
/// Reads and validates a federation registry workbook, producing the registered
/// athletes used for eligibility and matching. Implementations live in the
/// Infrastructure layer; Core depends only on this abstraction.
/// </summary>
public interface IRegistryReader
{
    /// <summary>
    /// Reads a registry workbook from a stream. The caller owns the stream. The
    /// returned result is never null: check <see cref="RegistryReadResult.IsValid"/>
    /// before using the athletes.
    /// </summary>
    RegistryReadResult Read(Stream stream);
}
