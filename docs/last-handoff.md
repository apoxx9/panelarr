# Last Handoff — 2026-10-14 (Session 33)

## Session 33 (10-14): both watch items verified, then the noise cleanup

A verification-only health check that closed out v1.1.47 and v1.1.48,
followed by **v1.1.49** for the log noise those checks exposed.

**The ended-tier dispersal is CONFIRMED.** The tier of 1,388 ended
series came due on 10-11 and now drains exactly **47/day** — which is
what `BudgetEndedTier` computes as `ceil(1388 / 30)`. Daily skip counts
fell 1,388 → ~1,343 in step. Scheduled refresh windows measured from
the logs: 0:51–1:12 before 10-11 (actives only), 1:47–2:06 after
(actives + 47 ended). Adding the ended tier costs about **50 seconds**.
The 11h30m Friday storms are structurally gone, not merely dormant.

**RSS paging is CONFIRMED**: every poll now reports 117–118 releases
found, up from the 36-post single page. The **safety net** is
registered and firing daily, but has had zero in-window missing issues
since 10-05, so it is still unproven under real load.

Coverage: all 15 October issues have files; every straggler from the
10-04 sweep (Monstress #63, Something Epic #13, Minor Arcana #17,
Nightwing #139, SIKTC #49, TMNT #20) **recovered itself via RSS** once
releases appeared — no intervention. 598 monitored-missing are
back-catalog (596 pre-2024). One current-run gap remains: **TMNT (2024)
#22** (released 09-18, added 09-17), searched manually on 10-14 with
zero releases on either indexer. It never got a safety-net attempt
because it was already 17 days old when v1.1.48 deployed on 10-04.

**v1.1.49 shipped (10-14)** — three noise sources, each of which hid
whether anything was actually wrong:

- **Archive signatures now beat extensions.** A `.cbr` holding a ZIP
  died on "Rar signature not found" and a `.cbz` holding RAR on
  "Failed to locate the Zip Header", leaving three library files
  permanently uninspected (MMPR Shattered Grid #01, MMPR/TMNT III #05,
  FF Masterworks #016). `ArchiveSignature.Identify` reads the magic
  bytes, and both `ArchiveInspector` and `ComicInfoReaderService` retry
  as the real format — but **only after the declared format throws**,
  so the happy path pays no extra open on NFS. No recognisable
  signature means no second guess, so a genuinely corrupt archive stays
  loud. EPUB is a ZIP container but never sniffs, because its handling
  cannot fail.
- **The vanished-mount guard and the empty-folder notice warn only when
  the folder has rows to lose** (`HasKnownFiles` →
  `GetFilesWithBasePath`). Twelve checklist volumes holding zero files
  were warning on every scan, which made the message useless for
  spotting the broken mount it exists to catch.
- **The recent-missing search logs at Info even when it searches
  nothing** — a quiet run was previously indistinguishable from a task
  that never fired, its only evidence a 61ms duration.

Suite: **2,710 Core.Test** green (+24) and Host.Test ContainerFixture
green. Verified by boot smoke test: the zero-case Info line appears
(`No missing issues released in the last 14 days to search`), UI routes
return 200 with zero console errors.

API GOTCHAS learned here: `/api/v1/series` does **not** expose
`lastInfoSync`, and `/api/v1/calendar` does **not** populate `hasFile`
(it leaves `seriesTitle` blank too) — use
`statistics.issueFileCount`. To measure the scheduled refresh window,
bracket the first→last `RefreshSeriesService|Skipping refresh of
series` timestamps per day; bracketing `Updating Info for` gives bogus
12–17h spans because `RefreshSeriesOnUnknownIssueReleaseService` fires
single-series refreshes from RSS all day long.

---

# Last Handoff — 2026-09-13 (Session 31)

## Session 31 (09-13): the refresh herd, found and fixed

The first steady-state log check (11 days after Session 30) found the
tiered refresh's flaw: the ended tier shares ONE interval and ONE
converged stamp date (the 08-20 status-derivation pass), so all
~1,300 ended series came due together — an **11h30m run with 135 CV
throttles every Friday**, between 1-minute daily runs. Zero errors
otherwise; RSS and the interactive lane were unaffected during it.

**v1.1.47 shipped (09-13):** ended series refresh **monthly** (a
revival is caught same-day by the RSS unknown-issue valve, then held
daily by its recent release — the monthly pass only freshens covers
and blurbs), and the scheduled run budgets the tier to its
steady-state rate (`ShouldRefreshSeries.BudgetEndedTier` = tier size /
interval days, oldest due first; ~43/day here). A bunched tier drains
evenly within one interval; no scheduled run can storm, structurally.
GOTCHA: **manual refreshes bypass the budget by design** — a manual
library-wide RefreshSeries is still the full storm; don't use it to
"test". VERIFY next session: daily runs ~43 series / ~13 min, no
Friday spike, backlog drained within ~30 days of 09-13.

Also noted: ComicVine delta sync (`GetChangedSeries`) remains a stub —
a researched future alternative, not a tweak.

---

# Session 30 handoff (2026-08-31, closed)

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

## Final log check (09-02, 48h of logs)

Clean. The single Error was GetComics delivering TMNT #21 as a RAR
mislabeled .cbz — the reader errored, the format converter detected
and repacked it 3s later, import succeeded (the error is cosmetic).
Warns: the unowned CA Epic checklist volumes (0 files, no folder)
trip the vanished-mount guard on every refresh rescan (noise); the
same 3 archives keep failing inspection: MMPR Shattered Grid #01,
MMPR/TMNT III #05, FF Masterworks #016 (possibly damaged rips).

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
6. Micro-polish (not release-worthy alone): vanished-mount warn →
   Debug when the series has zero files; ComicInfoReaderService could
   sniff RAR-as-.cbz before logging an Error. Optional: re-grab the 3
   inspection-failing archives above.
