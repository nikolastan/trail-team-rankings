using System.Net;
using TrailTeamRankings.Infrastructure.Racing;

namespace TrailTeamRankings.Tests.Racing;

public class RunTraceResultsProviderTests
{
    private static RunTraceResultsProvider NewProvider() => new(new HttpClient());

    // Trimmed RunTrace HTML that mirrors the real cell structure: js-*/td-* class
    // hooks, hidden helper spans (class "hide"), a category span with an id prefix,
    // and time cells marked colspan="td-def".
    private const string Fixture =
        """
        <html><head><title>RunTrace — Avala 2026</title></head><body>
        <table id="results-table" class="table table-datatable">
          <thead><tr>
            <th>Gen</th><th>Kat</th><th>Ime</th><th>Država</th><th>Broj</th>
            <th>Kategorija</th><th>Tim</th><th>KT1</th><th>KT2</th><th>Cilj</th>
            <th>Vreme</th><th>Tempo</th><th>Status</th><th></th><th></th>
          </tr></thead>
          <tbody>
            <tr>
              <td class="td-gen">1</td>
              <td class="td-cat">1</td>
              <td class="js-full_name td-name ta-left">Đuro Borbelj</td>
              <td class="td-country">Serbia</td>
              <td class="js-start_number td-start-number"><span class="hide">112</span><span class="td-start-color">112</span></td>
              <td class="td-category"><span class="hide">["3755"]</span><span class="td-category-color js-category" data-custom_categories='["3755"]'>Apsolutna M</span></td>
              <td class="js-team td-team"><span>PK Železničar Inđija</span></td>
              <td class="td-laps">0:43:15</td>
              <td class="td-laps">1:17:43</td>
              <td class="td-laps">1:25:23</td>
              <td colspan="td-def">1:25:23</td>
              <td colspan="td-def">04:45</td>
              <td class="js-status td-status-colors"><span class="status finished">Finished</span></td>
              <td class="td-certificate"></td>
              <td class="td-profile-link p-0"></td>
            </tr>
            <tr>
              <td class="td-gen">10</td>
              <td class="td-cat">1</td>
              <td class="js-full_name td-name ta-left">Ana Anić</td>
              <td class="td-country">Serbia</td>
              <td class="js-start_number td-start-number"><span class="hide">50</span><span class="td-start-color">50</span></td>
              <td class="td-category"><span class="hide">["3756"]</span><span class="td-category-color js-category">Apsolutna Ž</span></td>
              <td class="js-team td-team"><span>PK Tara</span></td>
              <td class="td-laps">0:50:00</td>
              <td class="td-laps">1:30:00</td>
              <td class="td-laps">1:40:00</td>
              <td colspan="td-def">1:40:00</td>
              <td colspan="td-def">05:30</td>
              <td class="js-status td-status-colors"><span class="status finished">Finished</span></td>
              <td class="td-certificate"></td>
              <td class="td-profile-link p-0"></td>
            </tr>
            <tr>
              <td class="td-gen">20</td>
              <td class="td-cat">2</td>
              <td class="js-full_name td-name ta-left">Marko Marković</td>
              <td class="td-country">Serbia</td>
              <td class="js-start_number td-start-number"><span class="hide">77</span><span class="td-start-color">77</span></td>
              <td class="td-category"><span class="hide">["3757"]</span><span class="td-category-color js-category">Juniori</span></td>
              <td class="js-team td-team"><span>PSK Balkan</span></td>
              <td class="td-laps">0:55:00</td>
              <td class="td-laps">1:45:00</td>
              <td class="td-laps">1:55:00</td>
              <td colspan="td-def">1:55:00</td>
              <td colspan="td-def">06:00</td>
              <td class="js-status td-status-colors"><span class="status finished">Finished</span></td>
              <td class="td-certificate"></td>
              <td class="td-profile-link p-0"></td>
            </tr>
            <tr>
              <td class="td-gen"></td>
              <td class="td-cat"></td>
              <td class="js-full_name td-name ta-left">Petar Perić</td>
              <td class="td-country">Serbia</td>
              <td class="js-start_number td-start-number"><span class="hide">88</span><span class="td-start-color">88</span></td>
              <td class="td-category"><span class="hide">["3755"]</span><span class="td-category-color js-category">Apsolutna M</span></td>
              <td class="js-team td-team"><span>PK Ozren</span></td>
              <td class="td-laps">0:44:00</td>
              <td class="td-laps"></td>
              <td class="td-laps"></td>
              <td colspan="td-def"></td>
              <td colspan="td-def"></td>
              <td class="js-status td-status-colors"><span class="status dnf">DNF</span></td>
              <td class="td-certificate"></td>
              <td class="td-profile-link p-0"></td>
            </tr>
            <tr class="odd"><td valign="top" colspan="15" class="dataTables_empty">No data</td></tr>
          </tbody>
        </table>
        </body></html>
        """;

    [Fact]
    public void Parse_ReadsRunners_WithCleanedCells()
    {
        var result = NewProvider().Parse(Fixture);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(4, result.Runners.Count);
        Assert.Equal("RunTrace — Avala 2026", result.RaceTitle);

        var first = result.Runners[0];
        Assert.Equal("Đuro Borbelj", first.Name);
        Assert.Equal("112", first.Bib);                    // "112 112" cleaned to "112"
        Assert.Equal("Apsolutna M", first.CategoryLabel);  // ["3755"] prefix stripped
        Assert.Equal("PK Železničar Inđija", first.Club);
        Assert.Equal("1:25:23", first.FinishTime);
        Assert.Equal("Finished", first.StatusText);
        Assert.Equal(1, first.OverallPlace);
        Assert.Equal(1, first.CategoryPlace);
    }

    [Fact]
    public void Parse_HandlesFemaleAndJuniorCategories()
    {
        var runners = NewProvider().Parse(Fixture).Runners;

        Assert.Equal("Apsolutna Ž", runners[1].CategoryLabel);
        Assert.Equal("Juniori", runners[2].CategoryLabel);
    }

    [Fact]
    public void Parse_HandlesDnfRow_WithMissingPlacesAndTime()
    {
        var dnf = NewProvider().Parse(Fixture).Runners[3];

        Assert.Equal("Petar Perić", dnf.Name);
        Assert.Equal("DNF", dnf.StatusText);
        Assert.Equal("88", dnf.Bib);
        Assert.Null(dnf.OverallPlace);
        Assert.Null(dnf.CategoryPlace);
        Assert.Null(dnf.FinishTime);
    }

    [Fact]
    public void Parse_SkipsRowsWithoutName()
    {
        var runners = NewProvider().Parse(Fixture).Runners;

        Assert.DoesNotContain(runners, r => string.IsNullOrWhiteSpace(r.Name));
        Assert.Equal(4, runners.Count); // the dataTables_empty placeholder row is ignored
    }

    [Fact]
    public void Parse_Fails_WhenNoResultsTable()
    {
        var result = NewProvider().Parse("<html><body><p>nothing here</p></body></html>");

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Parse_Fails_WhenNoRunnerRows()
    {
        var result = NewProvider().Parse("""<table id="results-table"><tbody></tbody></table>""");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("no runner rows"));
    }

    [Fact]
    public async Task GetResultsAsync_FetchesAndParses()
    {
        var provider = new RunTraceResultsProvider(
            new HttpClient(StubHandler.Returns(() => Ok(Fixture))));

        var result = await provider.GetResultsAsync("https://runtrace.net/avala2026");

        Assert.True(result.IsValid);
        Assert.Equal(4, result.Runners.Count);
    }

    [Fact]
    public async Task GetResultsAsync_ReturnsFailure_OnHttpError()
    {
        var provider = new RunTraceResultsProvider(
            new HttpClient(StubHandler.Throws(new HttpRequestException("boom"))));

        var result = await provider.GetResultsAsync("https://runtrace.net/avala2026");

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task GetResultsAsync_ReturnsFailure_OnNonSuccessStatus()
    {
        var provider = new RunTraceResultsProvider(
            new HttpClient(StubHandler.Returns(() => new HttpResponseMessage(HttpStatusCode.InternalServerError))));

        var result = await provider.GetResultsAsync("https://runtrace.net/avala2026");

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetResultsAsync_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var provider = new RunTraceResultsProvider(
            new HttpClient(StubHandler.Returns(() => Ok(Fixture))));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.GetResultsAsync("https://runtrace.net/avala2026", cts.Token));
    }

    private static HttpResponseMessage Ok(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body) };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _responder;

        private StubHandler(Func<HttpResponseMessage> responder) => _responder = responder;

        public static StubHandler Returns(Func<HttpResponseMessage> responder) => new(responder);

        public static StubHandler Throws(Exception exception) => new(() => throw exception);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_responder());
        }
    }
}
