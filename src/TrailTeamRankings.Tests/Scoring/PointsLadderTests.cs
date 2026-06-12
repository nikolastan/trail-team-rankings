using TrailTeamRankings.Core.Scoring;

namespace TrailTeamRankings.Tests.Scoring;

public class PointsLadderTests
{
    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 88)]
    [InlineData(3, 78)]
    [InlineData(4, 70)]
    [InlineData(5, 64)]
    [InlineData(10, 44)]
    [InlineData(30, 2)]
    [InlineData(31, 1)]
    public void GetPoints_ReturnsLadderValue_ForScoringPlaces(int place, int expected)
    {
        Assert.Equal(expected, PointsLadder.GetPoints(place));
    }

    [Theory]
    [InlineData(32)]
    [InlineData(50)]
    [InlineData(1000)]
    public void GetPoints_ReturnsZero_BeyondLadder(int place)
    {
        Assert.Equal(0, PointsLadder.GetPoints(place));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void GetPoints_Throws_ForPlaceBelowOne(int place)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PointsLadder.GetPoints(place));
    }

    [Fact]
    public void LastScoringPlace_Is31()
    {
        Assert.Equal(31, PointsLadder.LastScoringPlace);
    }

    [Fact]
    public void GetPoints_LadderStrictlyDecreases()
    {
        for (var place = 2; place <= PointsLadder.LastScoringPlace; place++)
        {
            Assert.True(
                PointsLadder.GetPoints(place) < PointsLadder.GetPoints(place - 1),
                $"Points for place {place} should be less than place {place - 1}.");
        }
    }
}
