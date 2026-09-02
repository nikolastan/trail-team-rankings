# Trail Team Rankings — Project Plan

Desktop WPF application that scrapes public race results from RunTrace, validates runner eligibility against a federation registry, computes club team standings (best 2 men + best 1 woman by points), and displays rankings in the UI with optional export to Excel and PDF.

---

## 1. Goals

| Goal | Description |
|------|-------------|
| **Primary** | Team rankings for **Seniori** and **Juniori**, ranked by sum of points (not finish times) |
| **Secondary** | Show individual results, all scraped runners, and excluded/ineligible athletes |
| **Output** | Export computed results to Excel (reference layout) and PDF |
| **Live use** | Manual refresh and optional polling during a race |

---

## 2. Data sources

### 2.1 User inputs (required)

| Input | Purpose | Gate |
|-------|---------|------|
| **Registry Excel** | Federation athlete register — eligibility (medical check) | **Mandatory** — rankings disabled until loaded and validated |
| **RunTrace URL** | Live or final race results | Required to compute rankings |
| **Race date** | Compare against medical `valid until` dates | Required for eligibility |

Example registry file: `_Базни камп Регистар спортиста - 2026-05-29 11-43 .xlsx`

Expected columns (row 2, Cyrillic):

- Име и презиме спортисте
- Датум рођења
- Основна организација
- Број такмичарске књижице
- ТЛС број
- Датум (продужења) регистрације
- Информације о члану
- Лекарски преглед

### 2.2 Scraped input

**RunTrace** — server-rendered HTML, **confirmed by recon** (July 2026): a plain GET
(no JavaScript) returns a fully populated jQuery **DataTables** table
(`<table id="results-table">`) with every runner row already in `<tbody>`. All rows
ship in one response (client-side paging only), so no pagination handling is needed.
Parsed with AngleSharp.

The live table has **15 columns**; we map the ones we need **by header text**
(order-independent, like the registry reader):

| # | Header | Use |
|---|--------|-----|
| 0 | Gen | Overall place |
| 1 | Kat | Category place |
| 2 | Ime | Runner name (Latin only — see 4.2) |
| 3 | Država | ignored |
| 4 | Broj | Bib — cell repeats the value; take the clean one. Race-specific, not used for matching |
| 5 | Kategorija | Category → division + gender. Cell also carries a `["id"]` prefix to strip |
| 6 | Tim | Club / team |
| 7–9 | KT1, KT2, Cilj | Checkpoint splits — ignored |
| 10 | Vreme | Finish time (display only) |
| 11 | Tempo | Pace — ignored |
| 12 | Status | Result status (see below) |
| 13–14 | — | Action icons — ignored |

Both **Gen** and **Kat** are captured; which one awards points is decided in the
pipeline (open question §12 #1), not in the scraper.

**Status vocabulary** (from the live filter): `Finished, Racing, Ready, OOR, DNF,
DISQ, DNS, Late, Registered, Pending, Rejected`. Only `Finished` scores; the rest are
surfaced in the Excluded panel. `Unknown` is the safe fallback for anything unrecognized.

### 2.3 Reference template (bundled, not user-uploaded)

**Points / results Excel** — e.g. `Rezultati-RtanjVertikala-2026.xlsx`

This file is **not** an input the user provides each session. It serves two purposes:

1. **Rules reference** — individual points ladder and column layout
2. **Export template** — shape of the Excel file the app **generates**

Store a copy under `docs/samples/` when added to the repo.

---

## 3. Scoring rules

### 3.1 Individual points ladder

Place → points for places 1–31:

```
100, 88, 78, 70, 64, 60, 56, 52, 48, 44, 40, 38, 36, 34, 32, 30,
28, 26, 24, 22, 20, 18, 16, 14, 12, 10, 8, 6, 4, 2, 1
```

Place 32+ → **0 points** (confirm with mentor).

### 3.2 Team scoring (Seniori and Juniori — identical logic)

For each club, within each division:

1. Consider only runners with status **Finished**
2. Consider only **eligible** runners (valid medical in registry)
3. Take best **2 males** and best **1 female** by **individual points** (highest point values win the slots)
4. **Team total** = sum of those three point values
5. Rank teams by total — **higher is better**
6. Missing gender slots count as **0** for that slot

### 3.3 Senior / Junior split

| Division | RunTrace categories (assumed) |
|----------|-------------------------------|
| **Seniori** | Apsolutna M, Apsolutna Ž, Veterani, Veteranke |
| **Juniori** | Juniori, Juniorke |

Run the full team pipeline **separately** per division. Do not mix categories across divisions.

**Open question:** Confirm with mentor whether Veterani/Veteranke belong under Seniori.

**Open question:** Confirm whether individual points come from **Gen** (overall) or **Kat** (category) place.

---

## 4. Eligibility and matching

### 4.1 Medical check

Registry column **Лекарски преглед** contains values like `важи до 11.09.2026`.

- **Eligible:** `valid until` date ≥ race date
- **Ineligible:** expired or missing medical info (default: exclude from team scoring)
- **Unmatched:** runner appears on RunTrace but has no registry row

### 4.2 Registry ↔ RunTrace matching

There is **no shared ID** (no JMBG on either side; TLS and booklet numbers exist only in registry; bib is race-specific).

| Strategy | Details |
|----------|---------|
| **Primary** | Normalized name match (Cyrillic ↔ Latin transliteration) |
| **Tie-breaker** | Club name fuzzy match |
| **Not used** | Bib, TLS, booklet for cross-system linking |

**Script bridge (mandatory).** Recon confirmed RunTrace serves names in **Latin only**
(`Đuro Borbelj`); there is no Cyrillic variant (`selected_lang=sr-Cyrl` → HTTP 500,
`sr` / `sr-Latn` → Latin). The registry is **Cyrillic** (`Ђуро Борбељ`). Matching
therefore *requires* Latin↔Cyrillic transliteration (near 1:1 Serbian mapping, incl.
digraphs `lj→љ nj→њ dž→џ` and diacritics `đ→ђ č→ч ć→ћ š→ш ž→ж`) before name comparison —
it is not optional.

### 4.3 Excluded runners (UI)

Ineligible and unmatched runners are **excluded from team math by default** but **always visible** in a dedicated UI tab/panel with reason:

- Medical expired
- No medical data
- Not found in registry
- Not finished (DNF/DNS/etc.)

---

## 5. User experience

### 5.1 Setup (gate)

```
Registry file:  [Browse…]  ✓ Loaded (N athletes)
Race URL:       [________________________]
Race date:      [DD.MM.YYYY]
```

Rankings views remain disabled until registry is loaded and passes format validation.

### 5.2 Main workspace

```
[Toolbar]
  [Refresh]   [Live ☐]   [Export ▼]   Last updated: HH:MM

[Tabs]
  Seniori teams | Juniori teams | Individuals | All runners | Excluded (N)
```

#### Seniori teams / Juniori teams (main feature)

| Rank | Klub | muš 1 | muš 2 | žena 1 | ukupno |

Optional row expand or detail pane: which runners contributed each slot.

#### Individuals (per division)

Side-by-side men and women tables:

| plasman | Ime i prezime | Klub | bodovi |

#### All runners

Full scraped list with eligibility badge and status.

#### Excluded

Filterable list with exclusion reason.

### 5.3 Live mode

| Mode | Behavior |
|------|----------|
| **Manual refresh** | Single GET + re-parse on button click |
| **Live** | Poll same URL every 30–60 s; stop when toggled off or window closes |
| **Threading** | Scrape on background thread; marshal results to UI |

Same scraper for post-race and live; only refresh interval differs.

### 5.4 Export

Export uses the **same in-memory result model** as the UI — no duplicate business logic.

**Menu:** Export ▼

| Format | v1 | Notes |
|--------|-----|-------|
| **Excel (.xlsx)** | Yes | Match reference template; Seniori + Juniori sheets; full EKIPNO block for **both** divisions |
| **PDF** | Yes | Printable report — team tables, optional individual summary |
| **CSV** | Later | Simple per-sheet dump |
| **Other** | Later | As needed |

**Export scope (user chooses):**

- Full report (both divisions)
- Current tab only (e.g. Seniori teams)
- Optional appendix: excluded runners

**Flow:**

```
Compute rankings → bind UI → user Export → pick format + scope → Save dialog
```

---

## 6. Generated Excel layout

Mirror `Rezultati-RtanjVertikala-2026.xlsx`. The sample Juniori sheet lacks a team block; **generated output must include it** for both divisions.

### Sheet: Seniori

| Block | Columns | Headers |
|-------|---------|---------|
| Men | A–D | plasman, Ime i prezime, Klub, bodovi |
| Women | G–J | plasman, Ime i prezime, Klub, bodovi |
| Teams | M–Q | Klub, muš 1, muš 2, žena 1, ukupno |

### Sheet: Juniori

Same structure as Seniori (including M–Q team block).

---

## 7. Architecture

### 7.1 Solution structure

```
trail-team-rankings/
├── docs/
│   ├── PLAN.md              ← this document
│   └── samples/             ← reference Excel, screenshots (later)
├── src/
│   ├── TrailTeamRankings.sln
│   ├── TrailTeamRankings.App/           # WPF, MVVM
│   ├── TrailTeamRankings.Core/          # models, rules, interfaces
│   ├── TrailTeamRankings.Infrastructure/ # scrape, Excel, PDF
│   └── TrailTeamRankings.Tests/         # xUnit
└── NuGet.Config
```

### 7.2 Layer responsibilities

| Project | Responsibility |
|---------|----------------|
| **Core** | `RaceRunner`, `ScrapedRunner`, `RegisteredAthlete`, `MedicalClearance`, `TeamStanding`, `DivisionResults`, `PointsLadder`, `TeamRankingService`, `CategoryDivisionMapper`, `GenderMapper`, `RaceStatusParser`, `MedicalClearanceParser`, status/eligibility enums |
| **Infrastructure** | `RunTraceResultsProvider` (AngleSharp), `RegistryExcelReader` (ClosedXML), `ExcelResultsExporter`, `PdfResultsExporter` |
| **App** | Views, ViewModels, setup gate, tabs, export dialog, `DispatcherTimer` for live poll |
| **Tests** | Ranking rules, points ladder, eligibility, edge cases (no network in CI) |

### 7.3 Key interfaces

```text
IRaceResultsProvider     → scrape RunTrace URL → raw scraped runners
IRegistryReader          → parse registry Excel → registered athletes
IRankingService          → eligible runners → DivisionResults
IResultsExporter         → DivisionResults → file (Excel, PDF, …)
```

**Rule:** Core does not reference WPF, HTTP, or Excel libraries.

### 7.4 Canonical result model

Single object produced by the pipeline and consumed by UI + all exporters:

```text
RaceResults
├── Seniori: DivisionResults
│   ├── TeamStandings[]
│   ├── MaleIndividuals[]
│   ├── FemaleIndividuals[]
│   └── ExcludedRunners[]
└── Juniori: DivisionResults
    └── (same shape)
```

### 7.5 Technology

| Area | Choice |
|------|--------|
| UI | WPF (.NET 10, `net10.0-windows`) |
| MVVM | CommunityToolkit.Mvvm |
| HTML scrape | HttpClient + AngleSharp |
| Excel read/write | ClosedXML |
| PDF export | QuestPDF (or equivalent — decide in Phase 8) |
| Tests | xUnit |

---

## 8. Implementation phases

| Phase | Goal | Deliverable |
|-------|------|-------------|
| **0** | Done | Solution skeleton, NuGet restore, build green |
| **1** | ✅ Done | `PointsLadder`, `TeamRankingService`, Senior/Junior split, unit tests |
| **2** | ✅ Done | `RegistryExcelReader`, medical validation, format validation |
| **3** | ✅ Done | Name normalization, club tie-break, excluded-runner reasons |
| **4** | ✅ Done | `RunTraceResultsProvider` — parse Avala URL into `ScrapedRunner` models |
| **5** | ✅ Done | Registry + scrape → `RaceResults` end-to-end (console + test harness) |
| **6** | ✅ Done | Setup gate, team tabs, individuals, all runners, excluded panel |
| **7** | ✅ Done | `ExcelResultsExporter` matching reference template |
| **8** | ✅ Done | `PdfResultsExporter` — printable team report |
| **9** | ✅ Done | Live poll, error banner, last-used paths (settings), README |

**Principle:** Ship vertical slices early. Phase 5 should produce correct `RaceResults` before investing heavily in UI polish.

### Phase 1 detail (done)

- `Runner`, `RegisteredAthlete`, `TeamStanding`, `IndividualResult`, `ExcludedRunner`
- `RaceStatus`, `EligibilityStatus`, `Division` enums
- `PointsLadder.GetPoints(int place)`
- `TeamRankingService.RankTeams(IEnumerable<ScoredRunner>, …)`
- xUnit: 2M+1F selection, incomplete teams, tie cases, Senior/Junior isolation

### Phase 4 detail (scraper)

- `RaceStatus`: add `Registered`, `Pending`, `Rejected` (full RunTrace vocabulary)
- `ScrapedRunner` raw model — `Name, Bib, CategoryLabel, Club, FinishTime, StatusText,
  OverallPlace?, CategoryPlace?` — eligibility / gender / division not yet resolved
- `GenderMapper` (category label → gender), beside `CategoryDivisionMapper`
- `RaceStatusParser` (status text → `RaceStatus`, `Unknown` fallback)
- `IRaceResultsProvider.GetResultsAsync(url, CancellationToken)` → `RaceScrapeResult`
  (runners + errors + metadata) — async for live polling
- `RunTraceResultsProvider` (AngleSharp): pure `Parse(html)` split from the `HttpClient`
  fetch; map 15 columns by header; clean the `Broj` / `Kategorija` cells
- xUnit on a trimmed HTML fixture (no live network in CI)

### Phase 6 detail (UI)

- `MainViewModel` holds `RaceResults?`
- Rankings `IsEnabled` bound to `RegistryLoaded`
- `DataGrid` per tab with sortable columns
- Status bar: last refresh time, error banner
- Busy indicator during scrape

---

## 9. Validation and errors

### 9.1 Registry file validation

On load, verify:

- Row 2 contains expected Cyrillic headers (fuzzy match)
- At least one data row present
- Medical column parseable for majority of rows

Fail with clear message if format does not match “Базни камп” export.

### 9.2 Points / export template validation

Reference sample in `docs/samples/` used in tests to ensure generated Excel structure stays aligned.

### 9.3 Scrape errors

- Network failure → retry message
- HTML layout change → parse error with row count 0 warning
- Empty results → inform user, do not clear previous data without confirmation (optional)

---

## 10. Configuration and persistence

Persist in user settings (`%AppData%` or similar):

- Last registry file path
- Last race URL
- Last race date
- Last export directory

Do **not** commit registry files or race exports to git.

---

## 11. Out of scope (v1)

- WebSocket / SignalR live feeds
- Additional result providers (other websites)
- User accounts / database
- Automatic download of registry or points files
- Individual-category rankings beyond display (e.g. veteran tables as separate standings)

Architecture should allow additional `IRaceResultsProvider` implementations later without renaming the solution.

---

## 12. Open questions for mentor

| # | Question | Assumption until confirmed |
|---|----------|----------------------------|
| 1 | Points from **Gen** or **Kat**? | Category place (Kat) |
| 2 | Veterani / Veteranke under **Seniori**? | Yes |
| 3 | Place 32+ → 0 points? | Yes |
| 4 | Registry export format always identical? | Yes — validate row 2 headers |
| 5 | PDF content: teams only or full report? | Teams + brief individual summary |

Document answers in this file or a future `docs/SDD.md` when confirmed.

**Confirmed by recon (July 2026):** RunTrace is server-rendered; categories are
`Apsolutna M/Ž, Juniori, Juniorke, Veterani, Veteranke`; the full status set is known
(11 values); names are Latin-only. Q1 (Gen vs Kat) stays open — the scraper captures
both, so switching is a one-line pipeline change.

---

## 13. Success criteria (v1 demo)

- [ ] User loads registry Excel; app validates and enables rankings
- [ ] User enters RunTrace URL and race date; Refresh populates data
- [ ] Seniori and Juniori team tabs show correct 2M+1F point totals
- [ ] Excluded tab lists ineligible/unmatched runners with reasons
- [ ] Export produces Excel matching reference layout (both sheets, team blocks)
- [ ] Export produces readable PDF of team standings
- [ ] Unit tests cover core ranking rules without network access

---

## 14. References

- RunTrace Avala 2026: `https://runtrace.net/avala2026?category_id=&race_id=1123&selected_lang=sr-Latn`
- Registry sample: `_Базни камп Регистар спортиста - 2026-05-29 11-43 .xlsx`
- Output template sample: `Rezultati-RtanjVertikala-2026.xlsx`

---

*Last updated: July 2026*
