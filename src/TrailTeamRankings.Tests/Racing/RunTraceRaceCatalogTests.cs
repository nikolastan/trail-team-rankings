using TrailTeamRankings.Core.Racing;
using TrailTeamRankings.Infrastructure.Racing;

namespace TrailTeamRankings.Tests.Racing;

public class RunTraceRaceCatalogTests
{
    private static RunTraceRaceCatalog NewCatalog() => new(new HttpClient());

    // Trimmed RunTrace home-page HTML: event cards carry data-slug_event / data-title
    // / data-status; the .race-date, .race-location and action button match the site.
    private const string Fixture =
        """
        <html><body>
        <div class="grid">
          <div class="grid__item js-event_thumb js-event_info"
               data-slug_event="rtanj2026" data-title="Rtanj Vertikal 2026"
               data-type="2" data-status="passed">
            <div class="grid__race__info">
              <a href="#" class="race-title">Rtanj Vertikal 2026</a>
              <div class="race-date">18.04.2026. 11:00</div>
              <div class="race-location"><i class="fas fa-map-marker-alt"></i> <span>Rtanj</span></div>
              <div class="btn-actions"><div class="col"><a href="/rtanj2026" class="btn" title="Results">Results</a></div></div>
            </div>
          </div>
          <div class="grid__item js-event_thumb js-event_info"
               data-slug_event="dayavala2026" data-title="ISKRA Trail Avala 2026"
               data-type="2" data-status="passed">
            <div class="grid__race__info">
              <a href="#" class="race-title">ISKRA Trail Avala 2026</a>
              <div class="race-date">30.08.2026. 08:00</div>
              <div class="race-location"><i class="fas fa-map-marker-alt"></i> <span>Avala</span></div>
              <div class="btn-actions"><div class="col"><a href="/dayavala2026" class="btn" title="Results">Results</a></div></div>
            </div>
          </div>
          <div class="grid__item js-event_thumb js-event_info"
               data-slug_event="bbkt2027" data-title="BBKT 2027"
               data-type="2" data-status="active">
            <div class="grid__race__info">
              <a href="#" class="race-title">BBKT 2027</a>
              <div class="race-date">10.01.2027. 12:00</div>
              <div class="race-location"><i class="fas fa-map-marker-alt"></i> <span>Smederevska Palanka</span></div>
              <div class="btn-actions">
                <div class="col"><a href="/bbkt2027/signup" class="btn" title="Sign up">Sign up</a></div>
                <div class="col"><a href="/bbkt2027/participants" class="btn" title="Participants">Participants</a></div>
              </div>
            </div>
          </div>
        </div>
        </body></html>
        """;

    [Fact]
    public void Parse_ReadsRaces_WithMetadata()
    {
        var result = NewCatalog().Parse(Fixture);

        Assert.True(result.IsValid);
        Assert.Equal(3, result.Races.Count);

        var rtanj = result.Races.Single(r => r.Slug == "rtanj2026");
        Assert.Equal("Rtanj Vertikal 2026", rtanj.Title);
        Assert.Equal(new DateOnly(2026, 4, 18), rtanj.Date);
        Assert.Equal("Rtanj", rtanj.Location);
        Assert.Equal(EventStatus.Finished, rtanj.Status);
        Assert.True(rtanj.HasResults);
        Assert.Equal("https://runtrace.net/rtanj2026", rtanj.ResultsUrl);
    }

    [Fact]
    public void Parse_MarksUpcomingRace_ActiveWithoutResults()
    {
        var race = NewCatalog().Parse(Fixture).Races.Single(r => r.Slug == "bbkt2027");

        Assert.Equal(EventStatus.Active, race.Status);
        Assert.False(race.HasResults); // only Sign up / Participants, no Results yet
    }

    [Fact]
    public void Parse_OrdersMostRecentFirst()
    {
        var races = NewCatalog().Parse(Fixture).Races;

        Assert.Equal(["bbkt2027", "dayavala2026", "rtanj2026"], races.Select(r => r.Slug));
    }

    [Fact]
    public void Parse_NoCards_ReturnsFailure()
    {
        var result = NewCatalog().Parse("<html><body><p>nothing here</p></body></html>");

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }
}
