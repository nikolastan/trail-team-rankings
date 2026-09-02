# Trail Team Rankings

A Windows desktop app that computes **club team standings** for Serbian trail‑running
races. It scrapes public race results from **RunTrace**, validates each runner's
eligibility against a **federation registry** (medical clearance), and ranks clubs by
the points of their best **2 men + 1 woman** — separately for the **Seniori** and
**Juniori** divisions. Results can be exported to **Excel** and **PDF**.

> University final‑exam project. The full specification lives in
> [`docs/PLAN.md`](docs/PLAN.md).

## Features

- Load a federation registry (`.xlsx`) and validate its format; rankings stay disabled
  until it loads.
- Scrape a RunTrace race URL (server‑rendered HTML, parsed with AngleSharp).
- Match scraped runners to registered athletes across scripts
  (**Serbian Latin ↔ Cyrillic** transliteration + name normalization).
- Resolve eligibility (valid medical on race day) and show **excluded** runners with a
  reason (not finished, not in registry, medical expired, no medical data).
- Team standings, individual standings (men/women), all runners, and an excluded panel.
- Manual **Refresh** and optional **Live** polling.
- **Excel** export mirroring the reference template, and a printable **PDF** report.
- Remembers your last registry, URL, race date, and export folder between sessions.

## Architecture

Layered, with the domain (`Core`) free of any I/O, UI, or third‑party frameworks.

| Project | Responsibility |
|---|---|
| `TrailTeamRankings.Core` | Domain models, scoring, ranking, matching/transliteration, results pipeline, and the abstractions (`IRegistryReader`, `IRaceResultsProvider`, `IResultsExporter`, `IUserSettingsStore`). |
| `TrailTeamRankings.Infrastructure` | Implementations: RunTrace scraper (AngleSharp), registry reader + Excel export (ClosedXML), PDF export (QuestPDF), JSON settings store. |
| `TrailTeamRankings.App` | WPF UI (MVVM via CommunityToolkit.Mvvm). |
| `TrailTeamRankings.Console` | Runnable end‑to‑end harness that prints standings. |
| `TrailTeamRankings.Tests` | xUnit tests (no network in CI). |

## Prerequisites

- **.NET 10 SDK** (the repo pins the version via [`global.json`](global.json)).

Verify with `dotnet --version` — it should report a `10.0.x` SDK.

## Build & test

```bash
dotnet build src/TrailTeamRankings.sln
dotnet test  src/TrailTeamRankings.Tests/TrailTeamRankings.Tests.csproj
```

## Run the app

```bash
dotnet run --project src/TrailTeamRankings.App
```

Then: **Browse** to a registry `.xlsx` → the race URL is pre‑filled → set the race date →
**Refresh**. Use **Export Excel…** / **Export PDF…** to save a report.

## Run the console harness

```bash
dotnet run --project src/TrailTeamRankings.Console -- <registry.xlsx> <raceUrl> [dd.MM.yyyy]
```

Prints team standings, individuals, and excluded counts for both divisions.

## Data & privacy

Federation registries contain athletes' personal data (names, birth dates, medical
status) and are **never committed** — they live under the git‑ignored
`docs/samples/private/`. The results template (`docs/samples/Rezultati‑…xlsx`) is public
and tracked. Generated exports are not committed.

## Tech stack

WPF (.NET 10) · CommunityToolkit.Mvvm · AngleSharp · ClosedXML · QuestPDF · xUnit.
