using TrailTeamRankings.Core.Text;

namespace TrailTeamRankings.Tests.Text;

public class NameNormalizerTests
{
    [Fact]
    public void Normalize_MatchesAcrossScripts()
    {
        Assert.Equal(
            NameNormalizer.Normalize("Ђуро Борбељ"),
            NameNormalizer.Normalize("Đuro Borbelj"));
    }

    [Fact]
    public void Normalize_IsWordOrderIndependent()
    {
        Assert.Equal(
            NameNormalizer.Normalize("Đuro Borbelj"),
            NameNormalizer.Normalize("Borbelj Đuro"));
    }

    [Fact]
    public void Normalize_FoldsDiacritics()
    {
        Assert.Equal(
            NameNormalizer.Normalize("Vučković"),
            NameNormalizer.Normalize("Vuckovic"));
    }

    [Fact]
    public void Normalize_IgnoresPunctuationAndExtraWhitespace()
    {
        Assert.Equal(
            NameNormalizer.Normalize("Petar Nikolić"),
            NameNormalizer.Normalize("  Nikolić,  Petar! "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_ReturnsEmpty_ForBlank(string? input)
    {
        Assert.Equal(string.Empty, NameNormalizer.Normalize(input));
    }
}
