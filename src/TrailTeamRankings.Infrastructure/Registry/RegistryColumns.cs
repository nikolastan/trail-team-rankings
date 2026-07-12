namespace TrailTeamRankings.Infrastructure.Registry;

/// <summary>
/// Expected column headers from the "Базни камп" federation registry export and
/// a normalizer used to match them tolerantly (case and surrounding whitespace
/// are ignored). Only <see cref="FullName"/> and <see cref="Medical"/> are
/// required for a file to be accepted.
/// </summary>
internal static class RegistryColumns
{
    public const string FullName = "Име и презиме спортисте";
    public const string Organization = "Основна организација";
    public const string BookletNumber = "Број такмичарске књижице";
    public const string TlsNumber = "ТЛС број";
    public const string Medical = "Лекарски преглед";

    /// <summary>
    /// Normalizes a header for comparison: trims, collapses internal whitespace
    /// runs to single spaces, and lower-cases. Returns an empty string for blanks.
    /// </summary>
    public static string Normalize(string? header) =>
        string.IsNullOrWhiteSpace(header)
            ? string.Empty
            : string.Join(
                ' ',
                header.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                .ToLowerInvariant();
}
