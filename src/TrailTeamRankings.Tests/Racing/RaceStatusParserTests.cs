using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Racing;

namespace TrailTeamRankings.Tests.Racing;

public class RaceStatusParserTests
{
    [Theory]
    [InlineData("Finished", RaceStatus.Finished)]
    [InlineData("Racing", RaceStatus.Racing)]
    [InlineData("Ready", RaceStatus.Ready)]
    [InlineData("OOR", RaceStatus.Oor)]
    [InlineData("DNF", RaceStatus.Dnf)]
    [InlineData("DISQ", RaceStatus.Disq)]
    [InlineData("DNS", RaceStatus.Dns)]
    [InlineData("Late", RaceStatus.Late)]
    [InlineData("Registered", RaceStatus.Registered)]
    [InlineData("Pending", RaceStatus.Pending)]
    [InlineData("Rejected", RaceStatus.Rejected)]
    public void Parse_MapsEveryKnownStatus(string text, RaceStatus expected)
    {
        Assert.Equal(expected, RaceStatusParser.Parse(text));
    }

    [Theory]
    [InlineData("finished")]
    [InlineData("  DnF  ")]
    [InlineData("oor")]
    public void Parse_IsCaseAndWhitespaceInsensitive(string text)
    {
        Assert.NotEqual(RaceStatus.Unknown, RaceStatusParser.Parse(text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Bananas")]
    public void Parse_ReturnsUnknown_ForBlankOrUnrecognized(string? text)
    {
        Assert.Equal(RaceStatus.Unknown, RaceStatusParser.Parse(text));
    }
}
