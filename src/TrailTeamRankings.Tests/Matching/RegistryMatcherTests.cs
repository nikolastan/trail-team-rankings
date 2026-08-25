using TrailTeamRankings.Core.Matching;
using TrailTeamRankings.Core.Models;

namespace TrailTeamRankings.Tests.Matching;

public class RegistryMatcherTests
{
    private static RegisteredAthlete Athlete(string name, string club) =>
        new(name, club, null, null, MedicalClearance.None);

    [Fact]
    public void TryMatch_MatchesAcrossScripts()
    {
        var matcher = new RegistryMatcher([Athlete("Ђуро Борбељ", "ПК Железничар")]);

        Assert.True(matcher.TryMatch("Đuro Borbelj", "PK Železničar Inđija", out var athlete));
        Assert.Equal("Ђуро Борбељ", athlete.FullName);
    }

    [Fact]
    public void TryMatch_ReturnsFalse_WhenNameAbsent()
    {
        var matcher = new RegistryMatcher([Athlete("Ђуро Борбељ", "ПК Железничар")]);

        Assert.False(matcher.TryMatch("Neko Drugi", null, out _));
    }

    [Fact]
    public void TryMatch_UsesClub_ToPickAmongHomonyms()
    {
        var matcher = new RegistryMatcher(
        [
            Athlete("Марко Марковић", "ПК Тара"),
            Athlete("Марко Марковић", "ПСК Балкан"),
        ]);

        Assert.True(matcher.TryMatch("Marko Marković", "PSK Balkan Beograd", out var athlete));
        Assert.Equal("ПСК Балкан", athlete.Organization);
    }
}
