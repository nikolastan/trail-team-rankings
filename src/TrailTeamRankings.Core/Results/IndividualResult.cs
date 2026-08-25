namespace TrailTeamRankings.Core.Results;

/// <summary>
/// One row in a division's individual standings ("plasman | Ime i prezime | Klub
/// | bodovi"): an eligible finisher with their points and 1-based placement.
/// </summary>
public sealed record IndividualResult(
    int Rank,
    string Name,
    string Club,
    int Points);
