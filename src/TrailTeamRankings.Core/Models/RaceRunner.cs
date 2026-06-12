namespace TrailTeamRankings.Core.Models;

/// <summary>
/// A single runner as fed into ranking: scraped result data combined with the
/// division, gender and eligibility resolved by earlier pipeline stages.
/// </summary>
/// <param name="Name">Runner full name.</param>
/// <param name="Club">Club / team name.</param>
/// <param name="Gender">Resolved gender, used to split counting runners per team.</param>
/// <param name="Division">Resolved division (Seniori / Juniori).</param>
/// <param name="Place">Finishing place used to award points.</param>
/// <param name="Status">Result status; only Finished runners are ranked.</param>
/// <param name="IsEligible">Whether the runner passed registry/medical checks.</param>
public sealed record RaceRunner(
    string Name,
    string Club,
    Gender Gender,
    Division Division,
    int Place,
    RaceStatus Status,
    bool IsEligible);
