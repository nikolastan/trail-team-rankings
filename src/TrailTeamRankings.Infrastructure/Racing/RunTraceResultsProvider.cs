using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Racing;

namespace TrailTeamRankings.Infrastructure.Racing;

/// <summary>
/// Scrapes a RunTrace results page into <see cref="ScrapedRunner"/> rows. RunTrace
/// serves a server-rendered jQuery DataTables table with every row embedded, so a
/// plain GET plus AngleSharp is enough. The network fetch is kept separate from
/// <see cref="Parse"/> so parsing can be unit tested on saved HTML with no network.
/// Cells are read via RunTrace's stable <c>js-*</c> / <c>td-*</c> classes rather
/// than by column index, and hidden helper spans (class <c>hide</c>) are stripped.
/// </summary>
public sealed class RunTraceResultsProvider : IRaceResultsProvider
{
    private const string DefaultUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) TrailTeamRankings/1.0";

    private readonly HttpClient _httpClient;
    private readonly HtmlParser _parser = new();

    public RunTraceResultsProvider(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<RaceScrapeResult> GetResultsAsync(
        string url, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        string html;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!request.Headers.UserAgent.TryParseAdd(DefaultUserAgent))
            {
                request.Headers.TryAddWithoutValidation("User-Agent", DefaultUserAgent);
            }

            using var response = await _httpClient
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw; // let cancellation propagate for clean live-poll shutdown
        }
        catch (Exception ex)
        {
            return RaceScrapeResult.Failure($"Could not fetch results from '{url}': {ex.Message}");
        }

        return Parse(html);
    }

    /// <summary>Parses RunTrace results HTML into scraped runners. Pure — no network.</summary>
    public RaceScrapeResult Parse(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        var document = _parser.ParseDocument(html);

        var table = document.QuerySelector("table#results-table");
        if (table is null)
        {
            return RaceScrapeResult.Failure(
                "No RunTrace results table (#results-table) was found in the page.");
        }

        var runners = new List<ScrapedRunner>();
        foreach (var row in table.QuerySelectorAll("tbody > tr"))
        {
            var name = CleanText(row.QuerySelector(".js-full_name"));
            if (string.IsNullOrWhiteSpace(name))
            {
                continue; // header, placeholder, or empty row
            }

            runners.Add(new ScrapedRunner(
                Name: name,
                Bib: NullIfEmpty(CleanText(row.QuerySelector(".js-start_number"))),
                CategoryLabel: CleanText(row.QuerySelector(".js-category") ?? row.QuerySelector(".td-category")),
                Club: NullIfEmpty(CleanText(row.QuerySelector(".js-team"))),
                FinishTime: NullIfEmpty(CleanText(row.QuerySelector("td[colspan='td-def']"))),
                StatusText: NullIfEmpty(CleanText(row.QuerySelector(".js-status"))),
                OverallPlace: ParseInt(CleanText(row.QuerySelector(".td-gen"))),
                CategoryPlace: ParseInt(CleanText(row.QuerySelector(".td-cat")))));
        }

        if (runners.Count == 0)
        {
            return RaceScrapeResult.Failure(
                "The RunTrace results table contained no runner rows (the layout may have changed).");
        }

        return RaceScrapeResult.Success(runners, NullIfEmpty(document.Title?.Trim() ?? string.Empty));
    }

    // Reads an element's visible text: drops hidden helper spans (class "hide")
    // and collapses whitespace runs to single spaces.
    private static string CleanText(IElement? element)
    {
        if (element is null)
        {
            return string.Empty;
        }

        foreach (var hidden in element.QuerySelectorAll(".hide").ToArray())
        {
            hidden.Remove();
        }

        return string.Join(
            ' ',
            element.TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static int? ParseInt(string value) =>
        int.TryParse(value, out var number) ? number : null;
}
