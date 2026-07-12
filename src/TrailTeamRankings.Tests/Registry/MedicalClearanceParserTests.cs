using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Registry;

namespace TrailTeamRankings.Tests.Registry;

public class MedicalClearanceParserTests
{
    [Fact]
    public void Parse_ExtractsValidUntilDate_FromTypicalText()
    {
        var clearance = MedicalClearanceParser.Parse("важи до 11.09.2026");

        Assert.True(clearance.HasClearanceDate);
        Assert.Equal(new DateOnly(2026, 9, 11), clearance.ValidUntil);
        Assert.Equal("важи до 11.09.2026", clearance.RawText);
    }

    [Theory]
    [InlineData("важи до 1.9.2026", 2026, 9, 1)]
    [InlineData("важи до 11.09.2026.", 2026, 9, 11)]
    [InlineData("  важи до 05.12.2025  ", 2025, 12, 5)]
    [InlineData("31.01.2027", 2027, 1, 31)]
    public void Parse_HandlesDigitAndWhitespaceVariants(
        string text, int year, int month, int day)
    {
        var clearance = MedicalClearanceParser.Parse(text);

        Assert.Equal(new DateOnly(year, month, day), clearance.ValidUntil);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_ReturnsNone_ForBlankInput(string? text)
    {
        var clearance = MedicalClearanceParser.Parse(text);

        Assert.Same(MedicalClearance.None, clearance);
        Assert.False(clearance.HasClearanceDate);
        Assert.Null(clearance.RawText);
    }

    [Theory]
    [InlineData("нема података")]
    [InlineData("важи до")]
    public void Parse_KeepsText_ButHasNoDate_WhenNoDatePresent(string text)
    {
        var clearance = MedicalClearanceParser.Parse(text);

        Assert.False(clearance.HasClearanceDate);
        Assert.Null(clearance.ValidUntil);
        Assert.Equal(text, clearance.RawText);
    }

    [Theory]
    [InlineData("важи до 31.02.2026")] // Feb 31 does not exist
    [InlineData("важи до 45.13.2026")] // impossible day and month
    public void Parse_RejectsImpossibleDates_ButKeepsText(string text)
    {
        var clearance = MedicalClearanceParser.Parse(text);

        Assert.False(clearance.HasClearanceDate);
        Assert.Equal(text, clearance.RawText);
    }

    [Fact]
    public void IsValidOn_IsTrue_OnOrBeforeValidUntil()
    {
        var clearance = MedicalClearanceParser.Parse("важи до 11.09.2026");

        Assert.True(clearance.IsValidOn(new DateOnly(2026, 7, 5)));   // before
        Assert.True(clearance.IsValidOn(new DateOnly(2026, 9, 11)));  // boundary
        Assert.False(clearance.IsValidOn(new DateOnly(2026, 9, 12))); // after
    }

    [Fact]
    public void IsValidOn_IsFalse_WhenNoDate()
    {
        Assert.False(MedicalClearance.None.IsValidOn(new DateOnly(2026, 7, 5)));
    }
}
