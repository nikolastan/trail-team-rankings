namespace TrailTeamRankings.Core.Models;

/// <summary>
/// A runner exactly as scraped from a single RunTrace results row, before any
/// interpretation. Gender, division, and eligibility are resolved later by the
/// pipeline; this type carries only raw source values. See <see cref="RaceRunner"/>
/// for the resolved, rank-ready form.
/// </summary>
/// <param name="Name">Runner name as shown on RunTrace (Latin script).</param>
/// <param name="Bib">Start number ("Broj"); race-specific, not used for matching.</param>
/// <param name="CategoryLabel">Category text ("Kategorija"), e.g. "Apsolutna M".</param>
/// <param name="Club">Club / team ("Tim").</param>
/// <param name="FinishTime">Finish time text ("Vreme"), display only.</param>
/// <param name="StatusText">Raw status text ("Status"), e.g. "Finished", "DNF".</param>
/// <param name="OverallPlace">Overall place ("Gen"), null when the cell is blank.</param>
/// <param name="CategoryPlace">Category place ("Kat"), null when the cell is blank.</param>
public sealed record ScrapedRunner(
    string Name,
    string? Bib,
    string CategoryLabel,
    string? Club,
    string? FinishTime,
    string? StatusText,
    int? OverallPlace,
    int? CategoryPlace);
