namespace TrailTeamRankings.Core.Models;

/// <summary>
/// A runner's medical clearance as recorded in the federation registry
/// (column "Лекарски преглед"). Holds the raw cell text plus, when it could be
/// parsed, the date the clearance is valid until.
/// </summary>
public sealed record MedicalClearance(string? RawText, DateOnly? ValidUntil)
{
    /// <summary>A clearance with no medical information at all.</summary>
    public static readonly MedicalClearance None = new(null, null);

    /// <summary>True when a valid-until date was successfully parsed.</summary>
    public bool HasClearanceDate => ValidUntil.HasValue;

    /// <summary>
    /// Whether the clearance is still valid on the given race date. A clearance
    /// with no parsed date is treated as not valid.
    /// </summary>
    public bool IsValidOn(DateOnly raceDate) =>
        ValidUntil is { } validUntil && validUntil >= raceDate;
}
