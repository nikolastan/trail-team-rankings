using System.Globalization;
using System.Text;
using TrailTeamRankings.Core.Results;
using TrailTeamRankings.Infrastructure.Racing;
using TrailTeamRankings.Infrastructure.Registry;

// Runnable end-to-end harness: registry + RunTrace scrape -> RaceResults, printed
// to the console. Usage:
//   dotnet run -- <registry.xlsx> <raceUrl> [raceDate dd.MM.yyyy]

if (args.Length < 2)
{
    Console.WriteLine("Usage: TrailTeamRankings.Console <registry.xlsx> <raceUrl> [raceDate dd.MM.yyyy]");
    return 1;
}

Console.OutputEncoding = Encoding.UTF8;

var registryPath = args[0];
var raceUrl = args[1];
var raceDate = args.Length >= 3
    && DateOnly.TryParseExact(args[2], "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
    ? parsed
    : DateOnly.FromDateTime(DateTime.Today);

Console.WriteLine($"Reading registry: {registryPath}");
var registry = new RegistryExcelReader().Read(registryPath);
if (!registry.IsValid)
{
    Console.WriteLine("Registry could not be loaded:");
    foreach (var error in registry.Errors) Console.WriteLine($"  - {error}");
    return 1;
}

Console.WriteLine($"  {registry.Athletes.Count} athletes loaded.");
Console.WriteLine($"Scraping: {raceUrl}");

using var http = new HttpClient();
var scrape = await new RunTraceResultsProvider(http).GetResultsAsync(raceUrl);
if (!scrape.IsValid)
{
    Console.WriteLine("Scrape failed:");
    foreach (var error in scrape.Errors) Console.WriteLine($"  - {error}");
    return 1;
}

Console.WriteLine($"  {scrape.Runners.Count} runners scraped.");

var results = RaceResultsBuilder.Build(registry.Athletes, scrape.Runners, raceDate, scrape.RaceTitle);

Console.WriteLine();
Console.WriteLine($"=== {results.RaceTitle ?? "Race"} — medical cutoff {results.RaceDate:dd.MM.yyyy} ===");
Console.WriteLine($"Mappable runners: {results.AllRunners.Count}");

PrintDivision(results.Seniori);
PrintDivision(results.Juniori);

if (results.UnclassifiedExcluded.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine($"Unclassified (unknown category): {results.UnclassifiedExcluded.Count}");
}

return 0;

static void PrintDivision(DivisionResults division)
{
    Console.WriteLine();
    Console.WriteLine($"################  {division.Division.ToString().ToUpperInvariant()}  ################");
    Console.WriteLine();
    Console.WriteLine("TEAM STANDINGS  (best 2 men + 1 woman by points)");
    Console.WriteLine($"{"#",-4}{"Club",-34}{"M1",5}{"M2",5}{"Ž",5}{"Total",7}");

    if (division.TeamStandings.Count == 0)
    {
        Console.WriteLine("  (no teams)");
    }

    foreach (var team in division.TeamStandings)
    {
        var m1 = team.CountingMales.Count > 0 ? team.CountingMales[0].Points.ToString() : "-";
        var m2 = team.CountingMales.Count > 1 ? team.CountingMales[1].Points.ToString() : "-";
        var female = team.CountingFemale?.Points.ToString() ?? "-";
        var incomplete = team.IsComplete ? "" : " *";
        Console.WriteLine($"{team.Rank,-4}{Trunc(team.Club, 33),-34}{m1,5}{m2,5}{female,5}{team.TotalPoints,7}{incomplete}");
    }

    Console.WriteLine("  (* = incomplete team; missing slots score 0)");

    Console.WriteLine();
    Console.WriteLine($"INDIVIDUALS  (men {division.MaleIndividuals.Count}, women {division.FemaleIndividuals.Count}) — top 5:");
    PrintTopIndividuals("Men", division.MaleIndividuals);
    PrintTopIndividuals("Women", division.FemaleIndividuals);

    Console.WriteLine();
    Console.WriteLine($"EXCLUDED: {division.ExcludedRunners.Count}");
    foreach (var group in division.ExcludedRunners.GroupBy(e => e.Reason).OrderByDescending(g => g.Count()))
    {
        Console.WriteLine($"  {group.Key,-16} {group.Count()}");
    }
}

static void PrintTopIndividuals(string label, IReadOnlyList<IndividualResult> individuals)
{
    Console.WriteLine($"  {label}:");
    if (individuals.Count == 0)
    {
        Console.WriteLine("    (none)");
        return;
    }

    foreach (var result in individuals.Take(5))
    {
        Console.WriteLine($"    {result.Rank,3}. {Trunc(result.Name, 26),-26} {Trunc(result.Club, 24),-24} {result.Points,4}");
    }
}

static string Trunc(string value, int max) =>
    value.Length <= max ? value : value[..(max - 1)] + "…";
