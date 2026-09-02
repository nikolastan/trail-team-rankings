using TrailTeamRankings.Core.Mapping;
using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Tests.Mapping;

public class GenderMapperTests
{
    [Theory]
    [InlineData("Apsolutna M", Gender.Male)]
    [InlineData("Apsolutna Ž", Gender.Female)]
    [InlineData("Juniori", Gender.Male)]
    [InlineData("Juniorke", Gender.Female)]
    [InlineData("Veterani", Gender.Male)]
    [InlineData("Veteranke", Gender.Female)]
    public void Map_ResolvesKnownCategories(string category, Gender expected)
    {
        Assert.Equal(expected, GenderMapper.Map(category));
    }

    [Theory]
    [InlineData("  apsolutna ž  ")]
    [InlineData("VETERANKE")]
    public void Map_IsCaseAndWhitespaceTolerant_ForFemale(string category)
    {
        Assert.Equal(Gender.Female, GenderMapper.Map(category));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Rekreativci")]
    [InlineData("Štafeta")]
    public void Map_ReturnsNull_ForUnknownOrEmpty(string? category)
    {
        Assert.Null(GenderMapper.Map(category));
    }

    [Fact]
    public void TryMap_ReturnsFalse_ForUnknown()
    {
        Assert.False(GenderMapper.TryMap("Rekreativci", out _));
    }

    [Theory]
    [InlineData("M Gen", Gender.Male)]
    [InlineData("Ž Gen", Gender.Female)]
    [InlineData("M 40-49", Gender.Male)]
    [InlineData("Ž 30-39", Gender.Female)]
    [InlineData("M Elite", Gender.Male)]
    [InlineData("Ž Elite", Gender.Female)]
    [InlineData("Seniori", Gender.Male)]
    [InlineData("Seniorke", Gender.Female)]
    public void Map_ResolvesPrefixedAndGeneralCategories(string category, Gender expected)
    {
        Assert.Equal(expected, GenderMapper.Map(category));
    }
}
