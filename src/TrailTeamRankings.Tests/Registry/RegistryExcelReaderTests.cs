using ClosedXML.Excel;
using TrailTeamRankings.Core.Registry;
using TrailTeamRankings.Infrastructure.Registry;

namespace TrailTeamRankings.Tests.Registry;

public class RegistryExcelReaderTests
{
    private readonly RegistryExcelReader _reader = new();

    // The eight registry headers in their expected order; medical is last.
    private static readonly string[] AllHeaders =
    [
        "Име и презиме спортисте",
        "Датум рођења",
        "Основна организација",
        "Број такмичарске књижице",
        "ТЛС број",
        "Датум (продужења) регистрације",
        "Информације о члану",
        "Лекарски преглед",
    ];

    [Fact]
    public void Read_ParsesAthletes_FromWellFormedRegistry()
    {
        using var stream = BuildWorkbook(ws =>
        {
            // Title row above the headers, to exercise header detection.
            ws.Cell(1, 1).Value = "Базни камп Регистар спортиста";
            WriteHeaders(ws, headerRow: 2);
            WriteAthlete(ws, 3, "Петар Петровић", "ПСД Авала", "1234", "TLS-1", "важи до 11.09.2026");
            WriteAthlete(ws, 4, "Ана Анић", "ПАК Полет", "5678", "TLS-2", "важи до 01.01.2027");
        });

        var result = _reader.Read(stream);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Athletes.Count);

        var petar = result.Athletes[0];
        Assert.Equal("Петар Петровић", petar.FullName);
        Assert.Equal("ПСД Авала", petar.Organization);
        Assert.Equal("1234", petar.BookletNumber);
        Assert.Equal("TLS-1", petar.TlsNumber);
        Assert.Equal(new DateOnly(2026, 9, 11), petar.Medical.ValidUntil);
    }

    [Fact]
    public void Read_MatchesColumnsByHeader_RegardlessOfOrder()
    {
        using var stream = BuildWorkbook(ws =>
        {
            // Reversed header order: medical first, name last.
            ws.Cell(1, 1).Value = "Лекарски преглед";
            ws.Cell(1, 2).Value = "Основна организација";
            ws.Cell(1, 3).Value = "Име и презиме спортисте";
            ws.Cell(2, 1).Value = "важи до 30.06.2026";
            ws.Cell(2, 2).Value = "ПСД Копаоник";
            ws.Cell(2, 3).Value = "Марко Марковић";
        });

        var result = _reader.Read(stream);

        var athlete = Assert.Single(result.Athletes);
        Assert.Equal("Марко Марковић", athlete.FullName);
        Assert.Equal("ПСД Копаоник", athlete.Organization);
        Assert.Equal(new DateOnly(2026, 6, 30), athlete.Medical.ValidUntil);
    }

    [Fact]
    public void Read_IsTolerant_OfHeaderCasingAndWhitespace()
    {
        using var stream = BuildWorkbook(ws =>
        {
            ws.Cell(1, 1).Value = "  ИМЕ И   ПРЕЗИМЕ СПОРТИСТЕ ";
            ws.Cell(1, 2).Value = "лекарски преглед";
            ws.Cell(2, 1).Value = "Јована Јовановић";
            ws.Cell(2, 2).Value = "важи до 15.08.2026";
        });

        var result = _reader.Read(stream);

        var athlete = Assert.Single(result.Athletes);
        Assert.Equal("Јована Јовановић", athlete.FullName);
        Assert.Equal(new DateOnly(2026, 8, 15), athlete.Medical.ValidUntil);
    }

    [Fact]
    public void Read_SkipsRowsWithoutName_AndWarns()
    {
        using var stream = BuildWorkbook(ws =>
        {
            WriteHeaders(ws, headerRow: 1);
            WriteAthlete(ws, 2, "Име Презиме", "Клуб", "1", "T1", "важи до 11.09.2026");
            // Row 3: no name, only a stray medical value.
            ws.Cell(3, ColumnOf("Лекарски преглед")).Value = "важи до 11.09.2026";
        });

        var result = _reader.Read(stream);

        Assert.True(result.IsValid);
        Assert.Single(result.Athletes);
        Assert.Contains(result.Warnings, w => w.Contains("skipped"));
    }

    [Fact]
    public void Read_Fails_WhenRequiredMedicalColumnMissing()
    {
        using var stream = BuildWorkbook(ws =>
        {
            ws.Cell(1, 1).Value = "Име и презиме спортисте";
            ws.Cell(1, 2).Value = "Основна организација";
            ws.Cell(2, 1).Value = "Петар Петровић";
            ws.Cell(2, 2).Value = "Клуб";
        });

        var result = _reader.Read(stream);

        Assert.False(result.IsValid);
        Assert.Empty(result.Athletes);
        Assert.Contains(result.Errors, e => e.Contains("Лекарски преглед"));
    }

    [Fact]
    public void Read_Fails_WhenNoDataRows()
    {
        using var stream = BuildWorkbook(ws => WriteHeaders(ws, headerRow: 1));

        var result = _reader.Read(stream);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("no athlete rows"));
    }

    [Fact]
    public void Read_Fails_ForNonExcelStream()
    {
        using var stream = new MemoryStream("this is not an xlsx"u8.ToArray());

        var result = _reader.Read(stream);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Read_Warns_WhenMostMedicalDatesUnreadable()
    {
        using var stream = BuildWorkbook(ws =>
        {
            WriteHeaders(ws, headerRow: 1);
            WriteAthlete(ws, 2, "A A", "Клуб", "1", "T1", "нема");
            WriteAthlete(ws, 3, "B B", "Клуб", "2", "T2", "нема");
            WriteAthlete(ws, 4, "C C", "Клуб", "3", "T3", "важи до 11.09.2026");
        });

        var result = _reader.Read(stream);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("medical clearance date"));
    }

    private static int ColumnOf(string header) => Array.IndexOf(AllHeaders, header) + 1;

    private static void WriteHeaders(IXLWorksheet worksheet, int headerRow)
    {
        for (var i = 0; i < AllHeaders.Length; i++)
        {
            worksheet.Cell(headerRow, i + 1).Value = AllHeaders[i];
        }
    }

    private static void WriteAthlete(
        IXLWorksheet worksheet,
        int row,
        string name,
        string organization,
        string booklet,
        string tls,
        string medical)
    {
        worksheet.Cell(row, ColumnOf("Име и презиме спортисте")).Value = name;
        worksheet.Cell(row, ColumnOf("Основна организација")).Value = organization;
        worksheet.Cell(row, ColumnOf("Број такмичарске књижице")).Value = booklet;
        worksheet.Cell(row, ColumnOf("ТЛС број")).Value = tls;
        worksheet.Cell(row, ColumnOf("Лекарски преглед")).Value = medical;
    }

    private static MemoryStream BuildWorkbook(Action<IXLWorksheet> build)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Регистар");
        build(worksheet);

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }
}
