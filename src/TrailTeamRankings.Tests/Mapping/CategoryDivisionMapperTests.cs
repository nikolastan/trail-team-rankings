using TrailTeamRankings.Core.Mapping;
using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Tests.Mapping;

public class CategoryDivisionMapperTests
{
    [Theory]
    [InlineData("Apsolutna M", Division.Seniori)]
    [InlineData("Apsolutna Ž", Division.Seniori)]
    [InlineData("Veterani", Division.Seniori)]
    [InlineData("Veteranke", Division.Seniori)]
    [InlineData("Seniori", Division.Seniori)]
    [InlineData("Juniori", Division.Juniori)]
    [InlineData("Juniorke", Division.Juniori)]
    public void Map_ResolvesKnownCategories(string category, Division expected)
    {
        Assert.Equal(expected, CategoryDivisionMapper.Map(category));
    }

    [Theory]
    [InlineData("  juniori  ")]
    [InlineData("JUNIORKE")]
    [InlineData("Juniori M")]
    public void Map_IsCaseAndWhitespaceTolerant(string category)
    {
        Assert.Equal(Division.Juniori, CategoryDivisionMapper.Map(category));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Štafeta")]
    [InlineData("Rekreativci")]
    public void Map_ReturnsNull_ForUnknownOrEmpty(string? category)
    {
        Assert.Null(CategoryDivisionMapper.Map(category));
    }

    [Fact]
    public void TryMap_ReturnsFalse_ForUnknownCategory()
    {
        Assert.False(CategoryDivisionMapper.TryMap("Rekreativci", out _));
    }

    [Fact]
    public void TryMap_ReturnsTrueAndDivision_ForKnownCategory()
    {
        Assert.True(CategoryDivisionMapper.TryMap("Apsolutna M", out var division));
        Assert.Equal(Division.Seniori, division);
    }
}
