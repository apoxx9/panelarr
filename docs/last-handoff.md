# Last Handoff — 2026-08-31 (Session 30, final)

Session 30 spanned 08-19 → 08-31 and shipped **three releases
(v1.1.43, v1.1.44, v1.1.45)**, closed the Session-24 library triage
(unmapped files 67 → 1), and confirmed the tiered refresh from v1.1.40
settled at ~47 min/run. One data-corruption bug was found the hard
way (a rescan re-homing another series' files), fixed across two
releases, and then hardened at its root (unpushed, see backlog).

## Releases shipped

- **v1.1.43** — Parser: `ParseIssueTitleWithSearchCriteria` returns
  null instead of throwing when a tracked download's series was
  deleted. **Retag events**: `ComicFileRetaggedEvent` is finally
  published (explicit retag commands only — never the import path,
  which already notifies); per-file events with a field-level
  ComicInfo diff → history rows + Kavita scan; published only AFTER
  every embed in the batch so Kavita's first scan sees final state;
  files whose tags are already current (and carry MetronInfo.xml) are
  skipped — no rewrite, no mtime churn. **Search false-grab fix**: an
  issue search for TMNT (2024) #21 grabbed the 2001 Mirage "Vol 4" MAM
  pack — v1.1.38's "series found, no issue → assume the search's
  issue" branch now consults the criteria parser (which refuses a
  volume marker that disagrees with the wanted number) and learned
  the subtitle-before-marker shape ("Aliens Epic Collection – The
  Original Years Vol. 3") that had forced the assumption.
- **v1.1.44** — Scan: only import decisions for paths under the
  walked folders; empty scan decides nothing. Add-series NRE:
  `FileNameBuilder.ReplaceToken` treats a null token as empty (posting
  a bare `{foreignSeriesId}` crashed `SeriesFolderAsRootFolderValidator`).
  Search: a release mapped to a DIFFERENT library series falls
  through to the specs and reads "Wrong series" again.
- **v1.1.45** — THE real rescan fix: `RescanFolders {seriesIds}` with
  no `folders` fell back to every ROOT folder with that series forced
  as the identification override — a 12k-file pass that re-homed two
  Convergence: JLA issues into Supergirl Annual (twice; repaired both
  times). `Scan()` now derives folders from the series ids and never
  falls back to the roots. Verified live: the same command completes
  in 3 s and touches nothing.

## Library work (Session-24 triage, CLOSED)

- **40 ComicVine annual volumes added** (unmonitored, no searches):
  DC files every annual as its own volume, and since 2021 as a
  one-shot "YYYY Annual" volume per year. ADD VIA THE FULL LOOKUP
  RESOURCE (as the UI does) — a minimal POST NREs on older builds.
- **8 mis-mapped files re-homed** (annuals squatting on issue #1
  slots or on the previous year's one-shot volume: Superman/Supergirl,
  JL 2022, JLD 2021, Bombshells, Injustice 2, Action Comics 2023
  (CV's name for the 2024 book), World's Finest 2025, Harley Quinn
  2022). Red Hood: Outlaw (2018) #41-49 imported → 24/24.
- **Duplicates/orphans deleted**: 10 dupes (~950 MB) + 5 orphans
  (~390 MB) incl. Resurrection Man's 880 KB cover-only "#1" replaced
  by the real issue. Remaining unmapped: exactly 1 — `Suske en Wiske
  #00 (1946).cbz` (kept on purpose; no CV issue).
- **Annual files moved into their own series folders** (45 files,
  filenames unchanged): the existing Rename Files flow relocates a
  file living outside its series folder even with `renameComics`
  OFF — no feature needed. Kavita's on-rename notify is off on the
  homelab → Kavita sees the moves on its next scan (read progress on
  those annuals resets). Kavita grouping follows the ComicInfo
  `Series` tag more than the folder — if it still lumps annuals under
  the parent, RETAG the annual series (previews first; user has not
  asked for this yet).
- Saved filters: "Complete Runs" (issueCount > 3), "Fragments (1–3
  issues)". FCBD/specials prune SKIPPED by user (11 series, ~1 GB
  stay). Library clutter review: user handles manually.
- Homelab: DirectDownload delay 60 min (Prefer Torrent). TMNT (2024)
  #21 grabbed from GetComics and imported 08-31 after an IssueSearch.

## Ops knowledge (hard-won this session)

- **Re-homing a mapped file = `ManualImport {importMode:'move',
  files:[{path, seriesId, issueId, quality}]}`** — it replaces the old
  row automatically. NEVER rescan to "clean up"; on builds < v1.1.45 a
  seriesIds-only rescan is a root scan with the series forced.
- The import path treats a file already under a library root as
  existing → ManualImport does NOT physically move it; use Rename
  Files (works with renameComics off) to relocate.
- Retag skip rule: a file is skipped when its generated ComicInfo
  equals the embedded one AND MetronInfo.xml is present.
- `SeriesFolderAsRootFolderValidator` builds the folder from the
  POSTED resource — send the full lookup resource when adding series.
- Homelab UI sits behind forms login (no creds here): puppeteer
  checks against the homelab are API-only; use the dev instance for UI.
- Trace-log pattern, stuck-import rescues, synchronous GC downloads,
  queue unknown-series rows, incremental-msbuild-skips-API,
  `broken_report_shouldnt_blowup` flake: unchanged from Session 29
  (see git history of this file at de29e1d).

## Backlog (agreed / open)

1. **Identification hardening (`82d30b5`) — SHIPPED as v1.1.46 (08-31).** The force-accept
   on a series override now demands the file be in the series folder,
   or delivered by a grab, or carry a tagged/parsed series title that
   fuzzy-matches the series (≥0.75). Makes the Convergence theft
   impossible even under a forced root scan.
2. Kavita: rescan the library after the annual moves; consider
   retagging the annual series if grouping is wrong (user decides).
3. Future annual downloads land in their own `… Annual (Year)/`
   folders — consistent with the moved files now.
4. Detective Comics 2023 Annual and Nightwing 2023 Annual have no
   ComicVine volume under any name tried; files deleted by user.
5. FlareSolverr-for-GC and the browser DDL resolver stay unbuilt /
   shelved.
