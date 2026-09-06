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

    [Fact]
    public void TryMatch_SubsetName_MatchesWhenClubAgrees()
    {
        var matcher = new RegistryMatcher([Athlete("Александра Мијановић", "ПК Тара, Бајина Башта")]);

        Assert.True(matcher.TryMatch("Aleksandra Coka Mijanović", "PK Tara Bajina Bašta", out var a));
        Assert.Equal("Александра Мијановић", a.FullName);
    }

    [Fact]
    public void TryMatch_SpellingVariant_MatchesWhenClubAgrees()
    {
        var matcher = new RegistryMatcher([Athlete("Викторија Келлер", "ПК Железничар, Инђија")]);

        Assert.True(matcher.TryMatch("Victoria Keller", "PK Železničar Inđija", out var a));
        Assert.Equal("Викторија Келлер", a.FullName);
    }

    [Fact]
    public void TryMatch_FuzzyRequiresClub()
    {
        var matcher = new RegistryMatcher([Athlete("Викторија Келлер", "ПК Железничар")]);

        Assert.False(matcher.TryMatch("Victoria Keller", null, out _));
    }

    [Fact]
    public void TryMatch_FuzzyRejectsDifferentClub()
    {
        var matcher = new RegistryMatcher([Athlete("Викторија Келлер", "ПК Железничар")]);

        Assert.False(matcher.TryMatch("Victoria Keller", "PSK Balkan Beograd", out _));
    }

    [Fact]
    public void TryMatch_DoesNotFuzzyMatchDifferentFirstNames()
    {
        // Same surname and club, but "Darko" is not a typo of "Marko" (no shared prefix).
        var matcher = new RegistryMatcher([Athlete("Марко Николић", "ПК Тара")]);

        Assert.False(matcher.TryMatch("Darko Nikolić", "PK Tara", out _));
    }
}
