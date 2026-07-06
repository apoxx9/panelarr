# Last Handoff — 2026-07-06 (End of Session 19)

Session 19 shipped **v1.1.6** (verified live on the homelab), closed the
whole homelab maintenance agenda (annuals mapped, redundant PDFs
deleted), settled the annuals + staging-import designs, and implemented
**staging-folder import Phase A** (backend, live-verified locally).
Suite: **2433 passed / 0 failed** at HEAD. Smoke: 24/24.

## Shipped — v1.1.6 (tagged, homelab verified on 1.1.6.82)

- **Ignored-download cache fix**: `TrackedDownloadService` handles
  `IssueGrabbedEvent` and evicts a cached tracked download in a terminal
  state (Ignored/Imported/Failed); the refresh already triggered by the
  grab rebuilds it from download history. Re-grabbing a removed
  torrent's infohash (cross-seed case from Session 18) resurfaces
  immediately, no restart. Verified live: grab SIKTC #48 → remove from
  queue keep-in-client → re-grab same DownloadId → item reappeared.
  (Upstream Sonarr has the identical bug, unfixed — checked.)
- **Issues-endpoint DB pagination** (last scale P1): page/pageSize on
  the unfiltered `GET /api/v1/issue` now run ORDER BY Id LIMIT/OFFSET
  in SQL via the existing PagingSpec infra instead of loading the whole
  table. Plain-array response shape unchanged; PagingResource envelope
  deliberately deferred to scale item #5. Live-verified byte-identical
  resources.
- **Rate-limiter test fix**: the two v1.1.4 throttle tests deliberately
  trigger the throttle Warn; the framework fails unexpected warns —
  but only in Debug builds (LoggingTest gates on BuildInfo.IsDebug),
  which is why Release/earlier runs were green. Fixed with
  `IgnoreWarns()` (ExceptionVerification's static capture count is
  unreliable — 0/1/2 copies of one warn observed — so ExpectedWarns(1)
  would itself flake).

## Homelab maintenance (all done, live)

- **Annuals decision + execution**: separate series per CV annual
  volume (merge toggle rejected: number-only matching would make
  Annual #1 vs Standard #1 ambiguous; see feature-landscape.md, which
  also gained backlog item #11 "related-series link" for UI grouping).
  Mapped cv:93388 / cv:101784 / cv:110105 (MMPR 2016/2017/2018
  Annuals), imported the 3 files from the parent MMPR (2016) folder,
  relocated into their own folders, quality Archive, all 1/1.
- **4 redundant PDFs deleted** from "MMPR: The Return" (~495 MB) via
  the local /Volumes/data mount; each had its tracked .cbz. 4/4 clean.
- **SIKTC #48 imported** as a byproduct of the cache-fix verification
  (wanted missing 3 → 2).

## Staging-folder import — design settled, Phase A shipped

Design decisions recorded in docs/staging-folder-import.md (move
default + keep-source-copy option; rename per naming settings like the
download pipeline; per-file decision-engine collision handling; both a
StagingFolder setting AND ad-hoc picker; publisher-folder normalization
included). Phase A (f3dd568 + f65eb45):

- **Publisher-folder normalization**: SeriesPathBuilder adopts an
  existing parent folder matching case/punctuation-insensitively
  (fixes the `Boom! Studios` vs `Boom Studios` split seen live when
  adding the annuals; the series-leaf component is never rewritten).
  AddSeriesService now delegates to SeriesPathBuilder (duplication
  removed) — adds, moves, bulk edits all normalized.
- **`GET /libraryimport/proposal?folder=`** scans an arbitrary staging
  folder from disk (same cvinfo/tags/name-search identification);
  folders inside a root folder → 400.
- **`StagingImport` command**: ensure series (create under target root
  + synchronous refresh via new `RefreshSeriesService.RefreshSeries`),
  regular import-decision engine, Move (or Copy w/ KeepSourceFiles),
  rejected files stay in staging, per-series ProgressInfo report.
- **StagingFolder setting** in media-management config (validated like
  RecycleBin). Live e2e verified: staged tagged cbz → exact proposal →
  import → file moved+renamed under existing "Boom Studios".

**Phase B (frontend) is NOT started**: settings field, Library Import
page "Import from staging" action (proposal review + target root +
keep-source checkbox), report surface. Also note for Phase B UX: an
untagged file with a bare filename can fail the 80% fuzzy-match
threshold (long CV issue titles) and stays in staging — the UI should
surface per-file rejection reasons.

## Quirks discovered (not fixed, candidate items)

1. **Queue-removal/import race**: removing a completed queue item
   (DownloadIgnored written) does not cancel an already-dispatched
   import — observed live: ignore at 10:34:58, import still completed
   at 10:35:01. Benign for wanted issues; surprising otherwise.
2. **Common.Test HTTP tests** hit `panelarr.com` (Sonarr fork-rename
   fossil, doesn't resolve) — 3 tests fail on any dev machine; add to
   terminology/fossil debt. Note: the session's "suite green" metric is
   **Core.Test only**; Integration/Automation need a live instance.
3. GetComics `DownloadId` is deterministic (hash of download file
   path) — useful for future test harnesses.

## Next actions (suggested order)

1. **Staging import Phase B** (frontend) — design is settled, backend
   verified; docs/staging-folder-import.md has the sketch.
2. Consider whether Phase A alone warrants a v1.1.7 or wait for B.
3. Backlog: Publisher UI Tier 2B, related-series link (landscape #11),
   calendar → weekly pull list, terminology key-debt (130 keys +
   RTorrentSettings Music* fossils + panelarr.com test-domain fossil).
4. Standing watch: quality "Unknown" in the manual-import modal
   (unreproduced since Session 18) — screenshot + keep modal open +
   `/api/v1/manualimport?downloadId=` live.

## Parked decisions still open

- MetadataProfileId accepted but unused by LibraryImportCommand.
- "Pull List" naming collision (terminology audit §2.2).
- Terminology key-debt cleanup scope.

## Reading list

docs/staging-folder-import.md (design + Phase A state),
docs/scale-readiness.md (#2 and #3 now marked fixed),
docs/feature-landscape.md (annuals decision + item #11),
docs/publisher-ui.md, docs/feature-landscape.md.
