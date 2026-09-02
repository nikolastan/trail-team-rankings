# Category classification — research & design

Reference notes on how the app turns a **RunTrace category label** into a
**division** (Seniori / Juniori) and a **gender** (Male / Female). Written up so it
can be cited when describing the app's design and its limitations.

Related code: [`GenderMapper`](../src/TrailTeamRankings.Core/Mapping/GenderMapper.cs),
[`CategoryDivisionMapper`](../src/TrailTeamRankings.Core/Mapping/CategoryDivisionMapper.cs).
Related plan sections: [PLAN.md](PLAN.md) §3.3 (senior/junior split) and §12 (open questions).

---

## 1. The problem

Team rankings need every runner assigned to a **division** and a **gender**, but
RunTrace publishes only a free-text **category** per runner, and **the vocabulary
differs from race to race**. A mapper hard-coded to one race's categories silently
drops every runner in another race (they fall through to *UnknownCategory* and are
excluded, so the rankings come up empty even though the scrape succeeded).

## 2. Survey of live RunTrace categories (≈20 races, 2026 season)

Sampled from `runtrace.net` in August 2026. Four broad schemes emerged:

| Scheme | Example races | Category labels |
|--------|---------------|-----------------|
| **Federation (absolute + age)** | avala | `Apsolutna M`, `Apsolutna Ž`, `Veterani`, `Veteranke`, `Juniori`, `Juniorke` |
| **Federation (senior/junior)** | rtanj, bor | `Seniori`, `Seniorke`, `Juniori`, `Juniorke` (bor adds age bands) |
| **General only** | dayavala, nightavala, gucevo, plavikrug, radmilovac, rrun | `M Gen`, `Ž Gen` |
| **General + age brackets** | cacak, kostolac, kovin, kraljevo, leskovac, sabac, svilajnac, zemun | `M Gen`/`Ž Gen` plus `M 40-49`, `Ž 30-39`, `M 60+`, … |
| **Elite + age** | legionar | `M Elite`, `Ž Elite`, `M 18-30`, … |

Frequency of the most common labels across the sample: `M Gen` / `Ž Gen` appeared in
**13** races each; age brackets (`M 40-49`, `M 50-59`, `Ž 40-49`, …) in most of the
rest; the federation word-categories only in the two championship races.

**The unifying pattern:** in every non-federation scheme, **gender is the leading
token** — `M …` for men, `Ž …` for women — and the suffix (`Gen`, an age band,
`Elite`) is just a *sub-category*, not a division. Only the federation races have a
genuine junior/senior split.

## 3. The classification rules (implemented)

### Gender — `GenderMapper`

1. **Female** if the label contains `ž` (`Apsolutna Ž`, `Ž Gen`, `Ž 40-49`) **or**
   ends in `-ke` (`Seniorke`, `Juniorke`, `Veteranke`).
2. Otherwise **Male** if the first token is `M` (`M Gen`, `M 40-49`, `M Elite`)
   **or** it contains a male word (`apsolutna m`, `seniori`, `juniori`, `veterani`).
3. Otherwise **unrecognized**.

### Division — `CategoryDivisionMapper`

1. Contains `junior` → **Juniori**.
2. Otherwise, if a **gender could be resolved** (i.e. it is a real individual
   competitive category) → **Seniori** — the senior/open division. This folds
   `Seniori`, `Veterani`, `Apsolutna`, `M/Ž Gen`, age brackets, and `Elite` into
   Seniori.
3. Otherwise **unrecognized** → the runner is excluded as *UnknownCategory*.

Both are keyword-based, case- and whitespace-insensitive.

### Worked examples

| Category | Gender | Division |
|----------|--------|----------|
| `Apsolutna M` | Male | Seniori |
| `Apsolutna Ž` | Female | Seniori |
| `Veterani` / `Veteranke` | Male / Female | Seniori |
| `Seniori` / `Seniorke` | Male / Female | Seniori |
| `Juniori` / `Juniorke` | Male / Female | **Juniori** |
| `M Gen` / `Ž Gen` | Male / Female | Seniori |
| `M 40-49` / `Ž 30-39` | Male / Female | Seniori |
| `M Elite` / `Ž Elite` | Male / Female | Seniori |
| `Štafeta`, `Rekreativci` | — | **excluded** (UnknownCategory) |

Effect measured on the real night-Avala race (`M Gen`/`Ž Gen`): before the rule,
all 50 runners were *UnknownCategory* and nothing ranked; after, **0 unclassified**
— all 50 classify into Seniori.

## 4. Coverage and deliberate exclusions

- **Federation races** (avala, rtanj) get the full Seniori **and** Juniori split.
- **General / age-group races** collapse to a single **Seniori** field; **Juniori is
  empty** (those races have no junior category), which is correct for them.
- **Non-individual categories** with no gender marker (relays `Štafeta`, recreational
  `Rekreativci`) are intentionally **excluded** — they are not part of the team
  competition.

## 5. Known limitations & open questions

1. **Age brackets and points.** Points come from *category place* (`Kat`), which
   **restarts at 1 in every age bracket**. In a race with `M 40-49`, `M 50-59`, …
   several runners each score 100 for winning *their* bracket, so a club's team total
   can double-count strong age-group placings. This is the **Gen-vs-Kat** open
   question (PLAN §12 #1), amplified by age groups. Using overall place (`Gen`) would
   avoid it — but many general races omit an overall-place column.
2. **"Non-junior ⇒ Seniori" is an assumption.** Reasonable for `Gen`, debatable for
   age-group or `Elite` events, which aren't really the federation 2M+1F team format.
3. **Thin fields ⇒ few complete teams.** Team completion needs 2 eligible men + 1
   eligible woman from one club. Women are a small share of finishers, so complete
   teams are rare and **highly sensitive to the race date** (which decides medical
   validity). Example — rtanj 2026: 6 senior women finished, ~4 eligible, and only
   **one** club (PSK Mosor Niš) also had 2 eligible men, so at most **one** complete
   team, and a slightly different race date yields **zero**. This is real data, not a
   defect, but it means the team ranking is meaningful mainly for the larger
   federation championship fields.

## 6. Takeaway

The scraper works across RunTrace events; the *interpretation* is where events
differ. Anchoring gender on the `M`/`Ž` leading token (plus the federation words) and
treating every recognized non-junior category as Seniori lets the app classify almost
any race, while honestly acknowledging that the club-team model itself was designed
for the federation championship races.
