using ClosedXML.Excel;
using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Registry;

namespace TrailTeamRankings.Infrastructure.Registry;

/// <summary>
/// Reads the federation registry from a "Базни камп" Excel export using ClosedXML.
/// The header row is located by content (not a fixed row index) so a title row
/// above the headers is tolerated, and columns are matched by header text so
/// column order does not matter.
/// </summary>
public sealed class RegistryExcelReader : IRegistryReader
{
    private const int HeaderSearchRowLimit = 10;

    /// <summary>Reads a registry workbook from a file path.</summary>
    public RegistryReadResult Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = File.OpenRead(path);
        return Read(stream);
    }

    /// <inheritdoc />
    public RegistryReadResult Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch (Exception ex)
        {
            return RegistryReadResult.Failure(
                $"The file could not be opened as an Excel workbook: {ex.Message}");
        }

        using (workbook)
        {
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet is null)
            {
                return RegistryReadResult.Failure("The workbook contains no worksheets.");
            }

            if (!TryFindHeaderRow(worksheet, out var headerRow, out var columns))
            {
                return RegistryReadResult.Failure(
                    $"The file does not look like a federation registry export: could not " +
                    $"find the required '{RegistryColumns.FullName}' and " +
                    $"'{RegistryColumns.Medical}' column headers in the first " +
                    $"{HeaderSearchRowLimit} rows.");
            }

            return ReadAthletes(worksheet, headerRow, columns);
        }
    }

    private static bool TryFindHeaderRow(
        IXLWorksheet worksheet,
        out int headerRow,
        out IReadOnlyDictionary<string, int> columns)
    {
        var fullNameKey = RegistryColumns.Normalize(RegistryColumns.FullName);
        var medicalKey = RegistryColumns.Normalize(RegistryColumns.Medical);

        foreach (var row in worksheet.RowsUsed().Take(HeaderSearchRowLimit))
        {
            var map = MapHeaderCells(row);
            if (map.ContainsKey(fullNameKey) && map.ContainsKey(medicalKey))
            {
                headerRow = row.RowNumber();
                columns = map;
                return true;
            }
        }

        headerRow = 0;
        columns = new Dictionary<string, int>();
        return false;
    }

    private static Dictionary<string, int> MapHeaderCells(IXLRow row)
    {
        var map = new Dictionary<string, int>();
        foreach (var cell in row.CellsUsed())
        {
            var key = RegistryColumns.Normalize(cell.GetString());
            if (key.Length > 0)
            {
                // First occurrence wins if a header is duplicated.
                map.TryAdd(key, cell.Address.ColumnNumber);
            }
        }

        return map;
    }

    private static RegistryReadResult ReadAthletes(
        IXLWorksheet worksheet,
        int headerRow,
        IReadOnlyDictionary<string, int> columns)
    {
        int Column(string header) =>
            columns.TryGetValue(RegistryColumns.Normalize(header), out var index) ? index : 0;

        var fullNameColumn = Column(RegistryColumns.FullName);
        var organizationColumn = Column(RegistryColumns.Organization);
        var bookletColumn = Column(RegistryColumns.BookletNumber);
        var tlsColumn = Column(RegistryColumns.TlsNumber);
        var medicalColumn = Column(RegistryColumns.Medical);

        var athletes = new List<RegisteredAthlete>();
        var skippedRows = 0;
        var withoutMedicalDate = 0;

        foreach (var row in worksheet.RowsUsed())
        {
            if (row.RowNumber() <= headerRow)
            {
                continue;
            }

            var fullName = CellText(row, fullNameColumn);
            if (string.IsNullOrWhiteSpace(fullName))
            {
                skippedRows++;
                continue;
            }

            var medical = MedicalClearanceParser.Parse(CellText(row, medicalColumn));
            if (!medical.HasClearanceDate)
            {
                withoutMedicalDate++;
            }

            athletes.Add(new RegisteredAthlete(
                fullName,
                NullIfEmpty(CellText(row, organizationColumn)),
                NullIfEmpty(CellText(row, bookletColumn)),
                NullIfEmpty(CellText(row, tlsColumn)),
                medical));
        }

        if (athletes.Count == 0)
        {
            return RegistryReadResult.Failure(
                "The registry contains no athlete rows below the header row.");
        }

        var warnings = new List<string>();
        if (withoutMedicalDate > athletes.Count / 2)
        {
            warnings.Add(
                $"{withoutMedicalDate} of {athletes.Count} athletes have no readable " +
                $"medical clearance date; check that the correct file was loaded.");
        }

        if (skippedRows > 0)
        {
            warnings.Add($"{skippedRows} row(s) were skipped because they had no athlete name.");
        }

        return RegistryReadResult.Success(athletes, warnings);
    }

    private static string CellText(IXLRow row, int column) =>
        column == 0 ? string.Empty : row.Cell(column).GetString().Trim();

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
