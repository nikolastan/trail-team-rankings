using TrailTeamRankings.Core.Text;

namespace TrailTeamRankings.Tests.Text;

public class ClubNormalizerTests
{
    [Fact]
    public void Normalize_MergesDiacriticVariants()
    {
        Assert.Equal(
            ClubNormalizer.Normalize("PK Tara Bajina Bašta"),
            ClubNormalizer.Normalize("PK Tara Bajina Basta"));
    }

    [Fact]
    public void Normalize_IsCaseInsensitive()
    {
        Assert.Equal(
            ClubNormalizer.Normalize("PSK Balkan"),
            ClubNormalizer.Normalize("psk balkan"));
    }

    [Fact]
    public void Normalize_MergesCyrillicAndLatin()
    {
        Assert.Equal(
            ClubNormalizer.Normalize("ПК Тара"),
            ClubNormalizer.Normalize("PK Tara"));
    }

    [Fact]
    public void Normalize_IgnoresPunctuation()
    {
        Assert.Equal(
            ClubNormalizer.Normalize("PD Omorika Uzice"),
            ClubNormalizer.Normalize("PD \"Omorika\" Užice"));
    }

    [Fact]
    public void Normalize_KeepsDistinctClubsSeparate()
    {
        Assert.NotEqual(
            ClubNormalizer.Normalize("PSK Balkan"),
            ClubNormalizer.Normalize("PSK Balkan Beograd"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_ReturnsEmpty_ForBlank(string? club)
    {
        Assert.Equal(string.Empty, ClubNormalizer.Normalize(club));
    }
}
