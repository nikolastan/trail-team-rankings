using TrailTeamRankings.Core.Text;

namespace TrailTeamRankings.Tests.Text;

public class SerbianTransliteratorTests
{
    [Theory]
    [InlineData("Ђуро Борбељ", "Đuro Borbelj")]
    [InlineData("Никола Вучковић", "Nikola Vučković")]
    [InlineData("Његош", "Njegoš")]
    [InlineData("Џон Љуба", "Džon Ljuba")]
    [InlineData("Шћепан", "Šćepan")]
    public void ToLatin_ConvertsCyrillicIncludingDigraphs(string cyrillic, string expected)
    {
        Assert.Equal(expected, SerbianTransliterator.ToLatin(cyrillic));
    }

    [Fact]
    public void ToLatin_LeavesLatinUnchanged()
    {
        Assert.Equal("Miloš Rajičević", SerbianTransliterator.ToLatin("Miloš Rajičević"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToLatin_ReturnsEmpty_ForBlank(string? input)
    {
        Assert.Equal(string.Empty, SerbianTransliterator.ToLatin(input));
    }
}
