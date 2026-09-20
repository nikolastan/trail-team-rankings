# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A Windows WPF desktop app (.NET 10) that computes **club team standings** for Serbian
trail races: it scrapes public results from **RunTrace**, validates each runner against a
**federation registry** (medical clearance), and ranks clubs by the points of their best
**2 men + 1 woman**, split into **Seniori** and **Juniori** divisions, with Excel/PDF
export. Full spec: [`docs/PLAN.md`](docs/PLAN.md).

## Commands

The default working directory is `src/`. Primary shell is PowerShell; a Bash tool is also
available.

```bash
dotnet build src/TrailTeamRankings.sln
dotnet test  src/TrailTeamRankings.Tests/TrailTeamRankings.Tests.csproj
```

Run a single test / class (xUnit filter):

```bash
dotnet test --filter "FullyQualifiedName~RunTraceRaceCatalogTests.Parse_OrdersMostRecentFirst"
```

Run the app, or the end-to-end console harness:

```bash
dotnet run --project src/TrailTeamRankings.App
dotnet run --project src/TrailTeamRankings.Console -- <registry.xlsx> <raceUrl> [dd.MM.yyyy]
```

.NET 10 SDK is pinned via `global.json` (10.0.300, `rollForward: latestFeature`).

## Architecture: the results pipeline

The whole domain is orchestrated by one pure entry point,
`RaceResultsBuilder.Build(registry, scrapedRunners, raceDate)` (Core/Results). Understand
these stages together — a change in one usually implies the others:

1. **Resolve the field** — `RaceFieldResolver.Resolve` (Core/Matching): per scraped runner,
   map the RunTrace category label to a `Division` (`CategoryDivisionMapper`) and `Gender`
   (`GenderMapper`), parse finish status (`RaceStatusParser`), match to the registry
   (`RegistryMatcher`), and set eligibility = *matched AND medical valid on the race date*.
   Emits `ResolvedField` (resolved runners + excluded runners each carrying an
   `ExclusionReason`).
2. **Score** — `ChampionshipScorer.Score` per division (Core/Scoring): keep only eligible
   finishers with a place, rank them **among eligible finishers** per gender, and award
   `PointsLadder` points for that rank. Points are *not* the raw category place.
3. **Rank teams** — `TeamRankingService.RankTeams` (Core/Ranking): group by normalized club
   key, take best 2 males + best 1 female by points.
4. **Individual standings** come from the same scored set.

### Domain invariants (verified against the mentor's ground-truth spreadsheet)

- **Points = rank among eligible finishers**, compacted with no gaps — not the scraped
  category place.
- **`PointsLadder`**: 100, 88, 78, … down to 1 for places 1–31; **place 32+ scores 1
  participation point** (not 0).
- **Team ranking**: only **complete** teams (2M + 1F) get a rank number; **incomplete teams
  are listed after, unranked** (`TeamStanding.Rank == 0`). Display surfaces render rank 0 as
  blank.
- **Race date** is scraped from the RunTrace page and auto-fills the UI (user can override);
  it is the cutoff for the medical-validity check.

### Registry matching (Core/Matching + Core/Text)

Names are matched across scripts/spellings by normalization in `Core/Text`
(`SerbianTransliterator` Cyrillic↔Latin, `TextNormalization.Latinize` diacritic folding,
`NameNormalizer` token-sorts, `ClubNormalizer`). `RegistryMatcher` is two-pass: exact
normalized-name key (club breaks homonym ties), then a **club-gated** fuzzy/subset fallback
(one-token typo with shared prefix, or an inserted nickname) that refuses to guess when
ambiguous. The fuzzy pass needs a corroborating club, so races with no club column only get
exact matches.

### RunTrace scraping (Infrastructure/Racing)

Two AngleSharp-based classes, each with a pure `Parse(html)` method for tests:
- `RunTraceResultsProvider` — scrapes one race's results (`/{slug}`). Handles server-side
  pagination via `/ajax/resultspage` and extracts the race date from a `.date` span.
- `RunTraceRaceCatalog` — powers the race picker; fetches the events list filtered
  server-side (`?filters[status]=all&filters[type]=2&filters[id_countries]=197` → Serbian
  trail races) and parses `.grid__item.js-event_info` cards. A race's results URL is just
  `/{slug}`.

## Conventions & gotchas

- **Core is pure** — no I/O, UI, or third-party frameworks. Add abstractions in Core
  (`IRegistryReader`, `IRaceResultsProvider`, `IRaceCatalog`, `IResultsExporter`,
  `IUserSettingsStore`) and implement them in Infrastructure.
- **Two status enums, don't confuse them**: `Core.Models.RaceStatus` is a *runner's* finish
  status (Finished/DNF/…); `Core.Racing.EventStatus` is a *race's* Active/Finished state.
- **WPF/MVVM**: CommunityToolkit.Mvvm source generators (`[ObservableProperty]`,
  `[RelayCommand]`). There is **no DI container** — the `MainViewModel` is composed by hand
  in the `MainWindow` constructor; add new services there.
- **Investigating live data**: tests never rely on the network in CI, but hitting real
  RunTrace / the mentor's spreadsheet locally is done by writing a temporary `[Fact]`/
  `[Theory]` in the Tests project that writes a report to the session scratchpad, running it
  with `--filter`, then **deleting the temp file**. Committed tests use inline HTML fixtures.
- **Never commit registry files** — they hold athletes' personal/medical data and live under
  the git-ignored `docs/samples/private/`. The results template under `docs/samples/` is
  public and tracked.
- **Commits are the user's** — provide a one-line commit message; do not run `git commit` or
  `git push`.
