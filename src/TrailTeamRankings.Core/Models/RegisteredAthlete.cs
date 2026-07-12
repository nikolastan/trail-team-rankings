namespace TrailTeamRankings.Core.Models;

/// <summary>
/// One athlete row from the federation registry Excel. Only the fields the
/// pipeline needs are captured: the name and organization drive registry ↔
/// results matching, and the medical clearance drives eligibility.
/// </summary>
/// <param name="FullName">Athlete full name ("Име и презиме спортисте").</param>
/// <param name="Organization">Base organization / club ("Основна организација").</param>
/// <param name="BookletNumber">Competition booklet number ("Број такмичарске књижице").</param>
/// <param name="TlsNumber">TLS number ("ТЛС број").</param>
/// <param name="Medical">Parsed medical clearance ("Лекарски преглед").</param>
public sealed record RegisteredAthlete(
    string FullName,
    string? Organization,
    string? BookletNumber,
    string? TlsNumber,
    MedicalClearance Medical);
