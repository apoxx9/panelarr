# Last Handoff — 2026-10-21 (Session 34)

## Session 34 (10-21): v1.1.49 holding, two faults found, v1.1.50

A routine check that turned into two real diagnoses. v1.1.49 is still
clean across 10-17→21 — all five noise signals at **zero** through
three full library scans (1,429 + 1,426 + 1,426 series-scans), refresh
windows 1:42 / 2:00 / 1:48, skipped 1,343/day, ended-tier dispersal
stable ten days on, RSS 347 polls at 113–117 found. Health 0, queue 0.

Under that calm were **295 errors and 890 warnings**.

### Fault 1: a grab limit retried 96 times a day

One release — Monstress #64 — was grabbed and refused **every RSS cycle
for three and a half days** (10-17 16:46 → 10-21 01:49, ~92–96/day).

The backoff code existed and *did* fire; it simply could never engage.
Prowlarr answers a grab past the tracker limit with **HTTP 429 carrying
no `Retry-After`**, so `TooManyRequestsException.RetryAfter` was
`TimeSpan.Zero`, and `DownloadService` passed that straight to
`RecordFailure` as the minimum backoff. Zero means "no minimum", so the
block fell to escalation **level 1 = 60 seconds**
(`EscalationBackOff.Periods[1]`) — which expired fourteen minutes before
the next RSS sync, and that sync's success called `RecordSuccess`,
decrementing the level back to zero. **The ladder could never climb.**

Fix: a missing `Retry-After` now falls back to **one hour**, exactly
what `HttpIndexerBase` has always done for the same response on the
search path. Confirmed `IndexerFactory.RssEnabled()` →
`FilterBlockedIndexers`, so a real block holds: RSS skips the indexer,
no `RecordSuccess` fires, and repeats escalate to 3h/6h/12h.
Self-resolved before the fix when MAM's limit reset.

### Fault 2: the safety net cannot see a re-dated issue

The net's window is anchored to `ReleaseDate`, but **ComicVine revises
release dates**. A date revised onto a day already outside the window
makes an issue invisible to the net permanently.

Two live instances: **Catwoman #90** and **Nightwing #141**, both added
2026-08-19, both absent from the Oct 1–22 calendar on 10-14, both later
re-dated onto **2026-10-01** — twenty days stale. Both sat missing while
the net logged its zero case on 10-18/19/20/21. **Catwoman #90 was
available on an indexer the whole time**: a manual search grabbed it in
seconds. So the zero-case line is NOT proof of coverage.

Fix: `Issue.LastSearchTime` already exists and is persisted, so no
schema change. Issues that have **never been searched** are now
reconsidered out to `NeverSearchedMaxAgeDays = 90`; beyond that a gap is
deliberate and the ~600 back-catalogue issues stay untouched. Verified a
no-op today — the newest remaining missing issue is from 2026-03-01.

**v1.1.50 shipped.** Suite 2,715 Core.Test (+5) and Host.Test
ContainerFixture green; clean boot smoke test, zero errors.

CAVEAT on the new `DownloadService` warn: its text is NOT asserted, and
the reason turned out to matter. Warn capture via
`ExceptionVerification` is **fixture-dependent and silently
non-functional in some fixtures** — in `DownloadServiceFixture` even a
direct `TestLogger.Warn("probe")` inside the test captured nothing
(asserted 2, got 0), and `LogManager.Flush()` did not help, while the
same assertion works in `ScanFixture`. The subject does receive the
real `TestLogger` (`TestBase` does `Mocker.SetConstant(TestLogger)`), so
this is process/config state rather than a wrong logger reference. Note
too that `LoggingDownBase.AssertNoUnexpectedLogs()` only runs when
`BuildInfo.IsDebug` **and the test already passed**. All three 429 tests
therefore use `IgnoreWarns()` and assert the **backoff duration**, which
is the real contract. Lesson: never treat a log-text assertion as the
contract here, and do not "fix" the harness casually — it is shared by
every test in the suite.

TIMEZONE TRAP worth knowing: Panelarr's **log files are local time
(UTC+2)** while the **API** (`system/status`, `history`, `system/task`)
is **UTC**. Mixing them made it look like the Monstress retry loop
continued after the successful import. On one UTC timeline the failures
stop at 10-20 23:49 and the grab succeeds at 10-21 00:05 — the loop
ended sixteen minutes *before* the success, so there is no
post-import-retry bug. Convert before correlating.

One extra detail on the loop's cost: each cycle made **two** requests to
the indexer's download endpoint — a 500, then the 429 about three
seconds later — so the traffic was roughly 590 requests, not 295.

ACTION TAKEN: searched and imported Catwoman #90 and Nightwing #141
(now 90/90 and 141/141). Monitored-missing 599 → 597, and nothing from
the current run is missing.

---

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

### v1.1.49 verified live (10-15)

Deployed 10-15 05:03 (`Bootstrap ... Version 1.1.49.186`). Before/after
log counts across that boundary:

| signal | before 05:03 | after |
| --- | --- | --- |
| ArchiveInspector warns | 39 | **0** |
| inspection-error warns | 39 | **0** |
| vanished-mount warns | 24 | **0** |
| "Scan folder is empty" warns | 2 | **0** |
| ComicInfoReader errors | 1 | **0** |

**The three "corrupt" archives were never corrupt.** The first v1.1.49
inspection pass (05:14) processed all three with zero warnings and took
**2.8s** against v1.1.48's 0.1s instant-fail, and they no longer appear
in `GetFilesPendingInspection` — whose query is `ImageCount == 0`. That
is the decisive proof they got real page counts, because a failed
inspection leaves `ImageCount` at 0 and re-queues the file forever
(which is exactly why those warnings recurred ~5×/day).

The safety net also hit its non-zero branch for the first time:
`Searching for 1 missing issues released in the last 14 days` (09:55,
Radiant Black #44 released 10-14, 0 grabbed — no release exists yet).
Refresh cadence unchanged post-deploy: 2:01 window, 1,344 skipped.

GOTCHA: `InspectComicFiles` only walks files with `ImageCount == 0`, so
a fixed file cannot be re-inspected through that command. The API's
`comicfile` and `series` resources expose **no** inspection fields
(no `pageCount`, `imageCount`, or `lastInfoSync`) — inspection state has
to be read from the logs plus the pending count.

**TMNT (2024) #22** — the gap flagged on 10-14 as having no release on
either indexer — was grabbed by RSS at 10-14 18:21, under an hour after
the manual search came back empty. #23 followed on 10-15. The newest
monitored-missing issue is now Radiant Black #44 (released 10-14);
everything else is pre-March-2026 back-catalog.

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
