using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Racing;

namespace TrailTeamRankings.Infrastructure.Racing;

/// <summary>
/// Scrapes a RunTrace results page into <see cref="ScrapedRunner"/> rows. Some
/// races embed every runner in one server-rendered table; others use
/// <c>data-server-results</c> pagination that renders only the first page (50
/// rows) and serves the rest as JSON from <c>/ajax/resultspage</c>. This provider
/// handles both: it parses the initial page, and when the page metadata reports
/// more pages, fetches them and appends their rows. Cells are read via RunTrace's
/// stable <c>js-*</c> / <c>td-*</c> classes; hidden helper spans are stripped.
/// </summary>
public sealed class RunTraceResultsProvider : IRaceResultsProvider
{
    private const string DefaultUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) TrailTeamRankings/1.0";
    private const string DefaultPageEndpoint = "/ajax/resultspage";
    private const int MaxPages = 200; // safety cap against a runaway page-count

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
            html = await FetchAsync(url, ajax: false, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return RaceScrapeResult.Failure($"Could not fetch results from '{url}': {ex.Message}");
        }

        var document = _parser.ParseDocument(html);
        var table = document.QuerySelector("table#results-table");
        if (table is null)
        {
            return RaceScrapeResult.Failure(
                "No RunTrace results table (#results-table) was found in the page.");
        }

        var runners = new List<ScrapedRunner>();
        AddRows(table, runners);

        // Best-effort: pull the remaining pages of a server-paginated race. If a
        // page fetch fails we keep the rows we already have rather than fail.
        try
        {
            await AddRemainingPagesAsync(url, document, runners, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // ignore — return what we managed to collect
        }

        if (runners.Count == 0)
        {
            return RaceScrapeResult.Failure(
                "The RunTrace results table contained no runner rows (the layout may have changed).");
        }

        return RaceScrapeResult.Success(runners, NullIfEmpty(document.Title?.Trim() ?? string.Empty));
    }

    /// <summary>Parses a single RunTrace results HTML blob. Pure — no network.</summary>
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
        AddRows(table, runners);

        if (runners.Count == 0)
        {
            return RaceScrapeResult.Failure(
                "The RunTrace results table contained no runner rows (the layout may have changed).");
        }

        return RaceScrapeResult.Success(runners, NullIfEmpty(document.Title?.Trim() ?? string.Empty));
    }

    private async Task AddRemainingPagesAsync(
        string url, IDocument firstPage, List<ScrapedRunner> runners, CancellationToken cancellationToken)
    {
        var table = firstPage.QuerySelector("#table");
        var meta = firstPage.QuerySelector(".js-results-page-meta");
        if (table is null || meta is null)
        {
            return; // not a server-paginated race
        }

        if (!int.TryParse(meta.GetAttribute("data-page-count"), out var pageCount) || pageCount <= 1)
        {
            return;
        }

        var eventId = table.GetAttribute("data-event-id");
        var raceView = table.GetAttribute("data-race-view");
        if (string.IsNullOrEmpty(eventId) || string.IsNullOrEmpty(raceView))
        {
            return; // cannot address the pagination endpoint
        }

        var endpoint = table.GetAttribute("data-url");
        if (string.IsNullOrEmpty(endpoint))
        {
            endpoint = DefaultPageEndpoint;
        }

        var categoryId = table.GetAttribute("data-category-id") ?? "0";
        var lang = table.GetAttribute("data-selected-lang") ?? "sr-Latn";
        var baseUri = new Uri(url);

        for (var page = 2; page <= Math.Min(pageCount, MaxPages); page++)
        {
            var query =
                $"{endpoint}?event_id={Uri.EscapeDataString(eventId)}" +
                $"&race_view={Uri.EscapeDataString(raceView)}" +
                $"&category_id={Uri.EscapeDataString(categoryId)}" +
                $"&page={page}&selected_lang={Uri.EscapeDataString(lang)}";

            var json = await FetchAsync(new Uri(baseUri, query).ToString(), ajax: true, cancellationToken)
                .ConfigureAwait(false);

            var pageHtml = ExtractHtml(json);
            if (pageHtml is null)
            {
                break;
            }

            var pageTable = _parser.ParseDocument(pageHtml).QuerySelector("table#results-table");
            if (pageTable is null)
            {
                break;
            }

            AddRows(pageTable, runners);
        }
    }

    private static string? ExtractHtml(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.TryGetProperty("status", out var status) &&
                !string.Equals(status.GetString(), "OK", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return root.TryGetProperty("html", out var html) ? html.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<string> FetchAsync(string url, bool ajax, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!request.Headers.UserAgent.TryParseAdd(DefaultUserAgent))
        {
            request.Headers.TryAddWithoutValidation("User-Agent", DefaultUserAgent);
        }

        if (ajax)
        {
            request.Headers.TryAddWithoutValidation("X-Requested-With", "XMLHttpRequest");
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddRows(IElement table, List<ScrapedRunner> runners)
    {
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
    }

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
