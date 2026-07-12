using System.Globalization;
using System.Text.RegularExpressions;
using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Core.Registry;

/// <summary>
/// Parses the free-text medical exam cell ("Лекарски преглед") from the
/// federation registry into a <see cref="MedicalClearance"/>. Typical values
/// look like "важи до 11.09.2026" (valid until 11 Sep 2026); the parser simply
/// extracts the first <c>d.M.yyyy</c> date it finds and ignores surrounding text.
/// </summary>
public static partial class MedicalClearanceParser
{
    // A day.month.year date: 1-2 digit day and month, 4 digit year. Any
    // surrounding text (e.g. "важи до", a trailing dot) is ignored.
    [GeneratedRegex(@"(\d{1,2})\.(\d{1,2})\.(\d{4})")]
    private static partial Regex DatePattern();

    /// <summary>
    /// Parses a raw medical-clearance cell. Returns <see cref="MedicalClearance.None"/>
    /// for blank input, and a clearance with a null date (but preserved text)
    /// when no valid date can be read.
    /// </summary>
    public static MedicalClearance Parse(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return MedicalClearance.None;
        }

        var text = rawText.Trim();
        var match = DatePattern().Match(text);
        if (!match.Success)
        {
            return new MedicalClearance(text, ValidUntil: null);
        }

        var day = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var month = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var year = int.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

        return TryMakeDate(year, month, day, out var validUntil)
            ? new MedicalClearance(text, validUntil)
            : new MedicalClearance(text, ValidUntil: null);
    }

    private static bool TryMakeDate(int year, int month, int day, out DateOnly date)
    {
        date = default;

        if (year is < 1 or > 9999 || month is < 1 or > 12)
        {
            return false;
        }

        if (day < 1 || day > DateTime.DaysInMonth(year, month))
        {
            return false;
        }

        date = new DateOnly(year, month, day);
        return true;
    }
}
