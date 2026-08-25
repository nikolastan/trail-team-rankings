namespace TrailTeamRankings.Core.Models;

/// <summary>
/// Why a scraped runner does not count toward team standings. Shown in the
/// Excluded panel so the user can see exactly who was left out and why.
/// </summary>
public enum ExclusionReason
{
    /// <summary>Result status is not Finished (DNF, DNS, DISQ, …).</summary>
    NotFinished,

    /// <summary>No matching athlete was found in the federation registry.</summary>
    NotInRegistry,

    /// <summary>Matched in the registry, but the medical cell has no readable date.</summary>
    NoMedicalData,

    /// <summary>Matched in the registry, but the medical clearance expired before the race.</summary>
    MedicalExpired,

    /// <summary>The RunTrace category could not be mapped to a division/gender.</summary>
    UnknownCategory,
}
