using ClosedXML.Excel;
using TrailTeamRankings.Core.Ranking;
using TrailTeamRankings.Core.Results;

namespace TrailTeamRankings.Infrastructure.Export;

/// <summary>
/// Exports <see cref="RaceResults"/> to an .xlsx that mirrors the reference
/// template (Rezultati-…): one sheet per division, each with a men block (A–D),
/// a women block (G–J), and an EKIPNO team block (rank in L, M–Q). Unlike the
/// sample, the team block is written for <em>both</em> divisions and is correctly
/// ranked by total.
/// </summary>
public sealed class ExcelResultsExporter : IResultsExporter
{
    // Men block
    private const int ColMenRank = 1;    // A
    private const int ColMenName = 2;    // B
    private const int ColMenClub = 3;    // C
    private const int ColMenPoints = 4;  // D
    // Women block
    private const int ColWomenRank = 7;    // G
    private const int ColWomenName = 8;    // H
    private const int ColWomenClub = 9;    // I
    private const int ColWomenPoints = 10; // J
    // Team (EKIPNO) block
    private const int ColTeamRank = 12;   // L
    private const int ColTeamClub = 13;   // M
    private const int ColTeamMale1 = 14;  // N
    private const int ColTeamMale2 = 15;  // O
    private const int ColTeamFemale = 16; // P
    private const int ColTeamTotal = 17;  // Q

    private const int TitleRow = 2;
    private const int HeaderRow = 3;
    private const int FirstDataRow = 4;

    public string FileExtension => "xlsx";

    public void Export(RaceResults results, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(destination);

        using var workbook = new XLWorkbook();
        WriteDivisionSheet(workbook, "Seniori", results.Seniori);
        WriteDivisionSheet(workbook, "Juniori", results.Juniori);
        workbook.SaveAs(destination);
    }

    /// <summary>Convenience overload that writes to a file path.</summary>
    public void Export(RaceResults results, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = File.Create(path);
        Export(results, stream);
    }

    private static void WriteDivisionSheet(XLWorkbook workbook, string sheetName, DivisionResults division)
    {
        var sheet = workbook.Worksheets.Add(sheetName);

        WriteTitlesAndHeaders(sheet);
        WriteIndividuals(sheet, division.MaleIndividuals, ColMenRank);
        WriteIndividuals(sheet, division.FemaleIndividuals, ColWomenRank);
        WriteTeams(sheet, division.TeamStandings);
        Style(sheet);
    }

    private static void WriteTitlesAndHeaders(IXLWorksheet sheet)
    {
        sheet.Cell(TitleRow, ColMenRank).Value = "MUŠKARCI";
        sheet.Range(TitleRow, ColMenRank, TitleRow, ColMenPoints).Merge();
        sheet.Cell(TitleRow, ColWomenRank).Value = "ŽENE";
        sheet.Range(TitleRow, ColWomenRank, TitleRow, ColWomenPoints).Merge();
        sheet.Cell(TitleRow, ColTeamClub).Value = "EKIPNO";
        sheet.Range(TitleRow, ColTeamClub, TitleRow, ColTeamTotal).Merge();

        sheet.Cell(HeaderRow, ColMenRank).Value = "plasman";
        sheet.Cell(HeaderRow, ColMenName).Value = "Ime i prezime";
        sheet.Cell(HeaderRow, ColMenClub).Value = "Klub";
        sheet.Cell(HeaderRow, ColMenPoints).Value = "bodovi";

        sheet.Cell(HeaderRow, ColWomenRank).Value = "plasman";
        sheet.Cell(HeaderRow, ColWomenName).Value = "Ime i prezime";
        sheet.Cell(HeaderRow, ColWomenClub).Value = "Klub";
        sheet.Cell(HeaderRow, ColWomenPoints).Value = "bodovi";

        sheet.Cell(HeaderRow, ColTeamClub).Value = "Klub";
        sheet.Cell(HeaderRow, ColTeamMale1).Value = "muš 1";
        sheet.Cell(HeaderRow, ColTeamMale2).Value = "muš 2";
        sheet.Cell(HeaderRow, ColTeamFemale).Value = "žena 1";
        sheet.Cell(HeaderRow, ColTeamTotal).Value = "ukupno";
    }

    private static void WriteIndividuals(
        IXLWorksheet sheet, IReadOnlyList<IndividualResult> individuals, int rankColumn)
    {
        var row = FirstDataRow;
        foreach (var individual in individuals)
        {
            sheet.Cell(row, rankColumn).Value = individual.Rank;
            sheet.Cell(row, rankColumn + 1).Value = individual.Name;
            sheet.Cell(row, rankColumn + 2).Value = individual.Club;
            sheet.Cell(row, rankColumn + 3).Value = individual.Points;
            row++;
        }
    }

    private static void WriteTeams(IXLWorksheet sheet, IReadOnlyList<TeamStanding> teams)
    {
        var row = FirstDataRow;
        foreach (var team in teams)
        {
            sheet.Cell(row, ColTeamRank).Value = team.Rank;
            sheet.Cell(row, ColTeamClub).Value = team.Club;
            if (team.CountingMales.Count > 0)
            {
                sheet.Cell(row, ColTeamMale1).Value = team.CountingMales[0].Points;
            }

            if (team.CountingMales.Count > 1)
            {
                sheet.Cell(row, ColTeamMale2).Value = team.CountingMales[1].Points;
            }

            if (team.CountingFemale is not null)
            {
                sheet.Cell(row, ColTeamFemale).Value = team.CountingFemale.Points;
            }

            sheet.Cell(row, ColTeamTotal).Value = team.TotalPoints;
            row++;
        }
    }

    private static void Style(IXLWorksheet sheet)
    {
        var titleRow = sheet.Row(TitleRow);
        titleRow.Style.Font.Bold = true;
        titleRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        sheet.Row(HeaderRow).Style.Font.Bold = true;
        sheet.Columns(ColMenRank, ColTeamTotal).AdjustToContents();
    }
}
