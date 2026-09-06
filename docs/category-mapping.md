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

## 2. Measured category fields

Full fields pulled from `runtrace.net` (all result pages — the app fetches every
page, not just the first). Categories and counts are the complete finishers list.

| Race (complete field) | Total | Category breakdown |
|-----------------------|------:|--------------------|
| **avala2026** | 217 | Apsolutna M 123, Apsolutna Ž 46, Veterani 32, Veteranke 12, Juniori 3, Juniorke 1 |
| **rtanj2026** | 132 | Seniori 78, Seniorke 38, Juniori 10, Juniorke 6 |
| **zlatibor2026** | 112 | Seniori 66, Seniorke 26, Veterani 15, Veteranke 5 |
| **nightavala2026** | 81 | M Gen 58, Ž Gen 23 |

Two structural families appear:

- **Word categories** (avala, rtanj, zlatibor): `Apsolutna M/Ž`, `Seniori/Seniorke`,
  `Veterani/Veteranke`, `Juniori/Juniorke`. Gender is carried by the word (or the
  `-ke` feminine ending / an `Ž`); the division by the word.
- **Prefix categories** (nightavala): `M …` / `Ž …`, where the gender is the leading
  token and the remainder (`Gen`, and on other RunTrace events an age band like
  `40-49` or `Elite`) is a *sub-category*, not a division.

Note that the **presence of a junior category varies even within the word family**:
avala and rtanj have `Juniori`/`Juniorke`; zlatibor (seniors + veterans) has none.
The prefix races have no junior category at all.

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

Effect measured on the real night-Avala race (`M Gen`/`Ž Gen`, 81 finishers): before
the rule, every runner was *UnknownCategory* and nothing ranked; after, **0
unclassified** — all 81 classify into Seniori.

## 4. Coverage and deliberate exclusions

- **Races with a junior category** (avala, rtanj) get the full Seniori **and** Juniori
  split.
- **Races without a junior category** — the prefix races (`M/Ž Gen`) and word races
  like zlatibor (seniors + veterans) — collapse to a single **Seniori** field, and
  **Juniori is legitimately empty**. The app treats an empty division as "not
  contested" (its export sheet/section is omitted, and its tab shows a count of 0).
- **Non-individual categories** with no gender marker (relays `Štafeta`, recreational
  `Rekreativci`) are intentionally **excluded** — they are not part of the team
  competition.

## 5. Known limitations & open questions

1. **Age brackets and points.** Points come from *category place* (`Kat`), which
   **restarts at 1 in every age bracket**. In a race with `M 40-49`, `M 50-59`, …
   several runners each score 100 for winning *their* bracket, so a club's team total
   can double-count strong age-group placings. This is the **Gen-vs-Kat** open
   question (PLAN §12 #1), amplified by age groups. Using overall place (`Gen`) would
   avoid it — but many prefix races omit an overall-place column.
2. **"Non-junior ⇒ Seniori" is an assumption.** Reasonable for `Gen`, debatable for
   age-group or `Elite` events, which aren't really the federation 2M+1F team format.
3. **Thin women fields ⇒ few complete teams.** Team completion needs 2 eligible men +
   1 eligible woman from one club. Women are a minority of finishers — e.g. zlatibor
   31 of 112 (28%), rtanj 44 of 132 (33%) — and *eligible* women (registered with a
   valid medical) are fewer still, spread across many clubs. Complete teams are
   therefore relatively scarce and **sensitive to the race date**, which decides
   medical validity. This is real data, not a defect, but it means the club-team
   ranking is most meaningful for the larger federation championship fields.

## 6. Takeaway

The scraper works across RunTrace events; the *interpretation* is where events
differ. Anchoring gender on the `M`/`Ž` leading token (plus the federation words) and
treating every recognized non-junior category as Seniori lets the app classify almost
any race, while honestly acknowledging that the club-team model itself was designed
for the federation championship races.
