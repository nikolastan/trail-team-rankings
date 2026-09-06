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

    [Fact]
    public async Task GetResultsAsync_FetchesAllPages_WhenServerPaginated()
    {
        const string page1 =
            """
            <html><head><title>Paged Race</title></head><body>
            <div id="table" data-server-results="1" data-url="/ajax/resultspage"
                 data-event-id="516" data-race-view="1230" data-category-id="0" data-selected-lang="sr-Latn">
              <div class="js-results-page-meta" data-page="1" data-page-size="1" data-page-count="2" data-total="2"></div>
              <table id="results-table"><tbody>
                <tr><td class="td-cat">1</td>
                    <td class="js-full_name td-name">Runner One</td>
                    <td class="td-category"><span class="td-category-color js-category">M Gen</span></td>
                    <td class="js-team td-team"><span>Club A</span></td>
                    <td class="js-status td-status-colors"><span class="status finished">Finished</span></td></tr>
              </tbody></table>
            </div></body></html>
            """;

        const string page2Json =
            """
            {"status":"OK","html":"<table id=\"results-table\"><tbody><tr><td class=\"td-cat\">2</td><td class=\"js-full_name td-name\">Runner Two</td><td class=\"td-category\"><span class=\"td-category-color js-category\">M Gen</span></td><td class=\"js-team td-team\"><span>Club B</span></td><td class=\"js-status td-status-colors\"><span class=\"status finished\">Finished</span></td></tr></tbody></table>"}
            """;

        var requested = new List<string>();
        var handler = StubHandler.Route(request =>
        {
            requested.Add(request.RequestUri!.ToString());
            var body = request.RequestUri!.AbsolutePath.Contains("resultspage") ? page2Json : page1;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        });

        var result = await new RunTraceResultsProvider(new HttpClient(handler))
            .GetResultsAsync("https://runtrace.net/pagedrace");

        Assert.True(result.IsValid);
        Assert.Equal(["Runner One", "Runner Two"], result.Runners.Select(r => r.Name));
        Assert.Contains(requested, u => u.Contains("resultspage") && u.Contains("race_view=1230") && u.Contains("page=2"));
    }

    private static HttpResponseMessage Ok(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body) };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        private StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        public static StubHandler Returns(Func<HttpResponseMessage> responder) => new(_ => responder());

        public static StubHandler Route(Func<HttpRequestMessage, HttpResponseMessage> responder) => new(responder);

        public static StubHandler Throws(Exception exception) => new(_ => throw exception);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_responder(request));
        }
    }
}
