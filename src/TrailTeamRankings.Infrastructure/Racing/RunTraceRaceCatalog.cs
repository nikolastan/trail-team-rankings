using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using TrailTeamRankings.Core.Racing;

namespace TrailTeamRankings.Infrastructure.Racing;

/// <summary>
/// Lists RunTrace races so the UI can offer a picker instead of a raw URL. The
/// RunTrace home page renders each event as a <c>.grid__item.js-event_info</c>
/// card carrying <c>data-slug_event</c>, <c>data-title</c> and <c>data-status</c>;
/// the same URL accepts <c>filters[...]</c> query parameters to return a filtered
/// set server-side. This catalog requests Serbian trail events of every status
/// and parses those cards. A race's results page is simply <c>/{slug}</c>, the
/// form <see cref="RunTraceResultsProvider"/> already scrapes.
/// </summary>
public sealed class RunTraceRaceCatalog : IRaceCatalog
{
    private const string DefaultUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) TrailTeamRankings/1.0";
    private const string DefaultBaseUrl = "https://runtrace.net/";

    // Serbia (id_countries=197), Trail (type=2), every status (active + passed).
    private const string CatalogQuery =
        "?filters%5Bstatus%5D=all&filters%5Btype%5D=2&filters%5Bid_countries%5D=197";

    private static readonly Regex DatePattern = new(@"(\d{1,2})\.(\d{1,2})\.(\d{4})", RegexOptions.Compiled);

    private readonly HttpClient _httpClient;
    private readonly Uri _baseUri;
    private readonly HtmlParser _parser = new();

    public RunTraceRaceCatalog(HttpClient httpClient, string baseUrl = DefaultBaseUrl)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);
        _httpClient = httpClient;
        _baseUri = new Uri(baseUrl);
    }

    /// <inheritdoc />
    public async Task<RaceCatalogResult> GetRacesAsync(CancellationToken cancellationToken = default)
    {
        var url = new Uri(_baseUri, CatalogQuery).ToString();

        string html;
        try
        {
            html = await FetchAsync(url, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return RaceCatalogResult.Failure($"Could not fetch the race list from '{url}': {ex.Message}");
        }

        return Parse(html);
    }

    /// <summary>Parses a RunTrace events-page HTML blob into race listings. Pure — no network.</summary>
    public RaceCatalogResult Parse(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        var document = _parser.ParseDocument(html);
        var cards = document.QuerySelectorAll(".grid__item.js-event_info");

        var races = new List<RaceListing>();
        foreach (var card in cards)
        {
            var listing = ToListing(card);
            if (listing is not null)
            {
                races.Add(listing);
            }
        }

        if (races.Count == 0)
        {
            return RaceCatalogResult.Failure(
                "No races were found in the RunTrace page (the layout may have changed).");
        }

        // Most recent first; races without a parseable date sort last.
        races.Sort((a, b) => Nullable.Compare(b.Date, a.Date));

        return RaceCatalogResult.Success(races);
    }

    private RaceListing? ToListing(IElement card)
    {
        var slug = card.GetAttribute("data-slug_event")?.Trim();
        if (string.IsNullOrEmpty(slug))
        {
            return null; // cannot address a race without its slug
        }

        var title = card.GetAttribute("data-title")?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            title = card.QuerySelector(".race-title")?.TextContent.Trim() ?? slug;
        }

        var status = string.Equals(card.GetAttribute("data-status"), "passed", StringComparison.OrdinalIgnoreCase)
            ? EventStatus.Finished
            : EventStatus.Active;

        var date = ParseDate(card.QuerySelector(".race-date")?.TextContent);
        var location = NullIfEmpty(card.QuerySelector(".race-location span")?.TextContent.Trim());

        // A "Results" action is present once the race has results to scrape.
        var hasResults = card.QuerySelectorAll("a").Any(a =>
            Contains(a.GetAttribute("title"), "result") || Contains(a.TextContent, "result"));

        var resultsUrl = new Uri(_baseUri, Uri.EscapeDataString(slug)).ToString();

        return new RaceListing(slug, title, date, location, status, hasResults, resultsUrl);
    }

    private static DateOnly? ParseDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = DatePattern.Match(text);
        if (!match.Success)
        {
            return null;
        }

        var day = int.Parse(match.Groups[1].Value);
        var month = int.Parse(match.Groups[2].Value);
        var year = int.Parse(match.Groups[3].Value);
        return month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(year, month)
            ? new DateOnly(year, month, day)
            : null;
    }

    private async Task<string> FetchAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (!request.Headers.UserAgent.TryParseAdd(DefaultUserAgent))
        {
            request.Headers.TryAddWithoutValidation("User-Agent", DefaultUserAgent);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }

    private static bool Contains(string? haystack, string needle) =>
        haystack is not null && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
