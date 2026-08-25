using TrailTeamRankings.Core.Mapping;
using TrailTeamRankings.Core.Models;
using TrailTeamRankings.Core.Racing;

namespace TrailTeamRankings.Core.Matching;

/// <summary>
/// Fuses scraped runners with the registry: resolves each runner's division,
/// gender, place and eligibility, and reports who is excluded from team scoring
/// and why. This is the bridge between the RunTrace scrape and the ranking engine.
/// </summary>
public sealed class RaceFieldResolver
{
    private readonly RegistryMatcher _matcher;
    private readonly PlaceSource _placeSource;

    public RaceFieldResolver(
        IEnumerable<RegisteredAthlete> registry,
        PlaceSource placeSource = PlaceSource.CategoryPlace)
    {
        _matcher = new RegistryMatcher(registry);
        _placeSource = placeSource;
    }

    /// <summary>
    /// Resolves a scraped field as of <paramref name="raceDate"/> (used for the
    /// medical-validity check).
    /// </summary>
    public ResolvedField Resolve(IEnumerable<ScrapedRunner> scrapedRunners, DateOnly raceDate)
    {
        ArgumentNullException.ThrowIfNull(scrapedRunners);

        var runners = new List<RaceRunner>();
        var excluded = new List<ExcludedRunner>();

        foreach (var scraped in scrapedRunners)
        {
            var division = CategoryDivisionMapper.Map(scraped.CategoryLabel);
            var gender = GenderMapper.Map(scraped.CategoryLabel);
            var status = RaceStatusParser.Parse(scraped.StatusText);

            if (division is null || gender is null)
            {
                excluded.Add(new ExcludedRunner(
                    scraped.Name, scraped.Club, scraped.CategoryLabel,
                    division, status, ExclusionReason.UnknownCategory));
                continue;
            }

            var matched = _matcher.TryMatch(scraped.Name, scraped.Club, out var athlete);
            var isEligible = matched && athlete.Medical.IsValidOn(raceDate);
            var place = SelectPlace(scraped) ?? 0;

            runners.Add(new RaceRunner(
                scraped.Name,
                scraped.Club ?? string.Empty,
                gender.Value,
                division.Value,
                place,
                status,
                isEligible));

            var reason = DetermineExclusion(status, matched, matched ? athlete.Medical : null, raceDate);
            if (reason is not null)
            {
                excluded.Add(new ExcludedRunner(
                    scraped.Name, scraped.Club, scraped.CategoryLabel,
                    division, status, reason.Value));
            }
        }

        return new ResolvedField(runners, excluded);
    }

    private int? SelectPlace(ScrapedRunner scraped) =>
        _placeSource == PlaceSource.CategoryPlace ? scraped.CategoryPlace : scraped.OverallPlace;

    // Reason a runner does not count toward teams, or null if they do. Status
    // takes priority: a non-finisher is reported as "not finished" regardless of
    // registry state, since that is the operative reason they are out.
    private static ExclusionReason? DetermineExclusion(
        RaceStatus status, bool matched, MedicalClearance? medical, DateOnly raceDate)
    {
        if (status != RaceStatus.Finished)
        {
            return ExclusionReason.NotFinished;
        }

        if (!matched)
        {
            return ExclusionReason.NotInRegistry;
        }

        if (medical!.IsValidOn(raceDate))
        {
            return null; // eligible finisher — counts toward teams
        }

        return medical.HasClearanceDate
            ? ExclusionReason.MedicalExpired
            : ExclusionReason.NoMedicalData;
    }
}
