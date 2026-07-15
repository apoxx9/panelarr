# Last Handoff — 2026-07-15 (Session 23, final)

Session 23 spanned 07-09 → 07-15 and shipped FIVE releases
(v1.1.14 … v1.1.18). The Session 22 leftovers pass is essentially
complete: **unmapped went 619 → 143**, the Epic Collections are fully
modeled, and the library's data model now tells the truth about shared
folders. One staged action remains (see NEXT ACTIONS).

## Releases shipped

- **v1.1.14** — Comic-pack support (parser `IsMultiIssue`, decision-engine
  gate: packs rejected for RSS/auto, allowed interactive; multi-series
  download folders drop the series override and import per-file).
  Kavita notify fix (title was prefixed onto the scan-folder path —
  every notification 500'd; reproduced live, now bare path). Broken
  Donations section removed. **Async archive inspection**: page-count/
  quality + credit extraction moved off the import event path into
  queued InspectComicFilesCommand (query-driven: mapped files with
  ImageCount 0; one archive open per file; ComicInfo captured in the
  same pass — the Session 22 "scan hang" fix proper).
- **v1.1.15** — Session 22 bug batch: CommandEqualityComparer null
  short-circuit (distinct commands deduped away — root cause of the
  "vanished on restart" myth: Requeue() was proven fine by seeded-DB
  test; the rescans requeued but ran as no-ops due to…) +
  FilterFilesType.Matched fix (unmapped rows were treated as matched —
  now IssueId>0 is authoritative) + vanished-mount guard (missing
  series folder skips DB cleanup instead of purging rows).
- **v1.1.16** — Identification threshold fixes (traced live: correct
  matches scored 79.2% vs 80% cliff): placeholder titles ("#18") no
  longer compared against provider arc titles; publisher alias
  containment (DC vs DC Comics) = match; candidate number fallback uses
  ComicParser (the "Season 10 #025 → issue 10" trap); tag ids embedded
  in ≥3 files are distrusted and those files identify by filename
  (observed: 18 files all tagged as issue #2 by a Mylar accident).
- **v1.1.17** — **Shared folders first-class**: explicit import paths
  that exist on disk are respected (AddSeries no longer rebinds to
  disambiguated phantom paths — 92 series were silently broken that
  way); SeriesPathValidator accepts shared paths for existing folders;
  MoveSeries refuses shared sources (delete-files guard already
  existed). Scan pipeline audited: already shared-path-safe.
- **v1.1.18** — **ConvertComicFilesCommand** (Mylar's convert-before-tag
  with verification): RAR/7z repacked to real CBZ via .partial~ temp +
  entry-count verification before the original is deleted; zip content
  with wrong extension renamed; DB rows updated; mapped files get
  ComicInfo/MetronInfo embedded. Sweeps every file under the target
  series' folders (covers unmapped mislabeled rows).

## Homelab state at handoff

- Library: **1,353 series, 143 unmapped**, health clean, all paths true.
- 45 DC annual volumes + 47 Epic Collection one-shots imported and
  mapped (annuals via fixed proposal scan; Epics: 22 by tag-id rescan +
  25 by deterministic manual import). All Epic-named series carry the
  **epic-collection tag (tag id 1)** for filtering.
- **Reading lists** (volume-ordered, all slots CV-id-resolved):
  id 4 = ASM Epic Collections (22, PUSHED to Kavita 22/22),
  id 5 = FF (14), id 6 = Iron Man (11) — pushes REJECTED by Kavita
  ("series missing"): those files are untagged .cbr, so Kavita groups
  them by filename into one series per line. Fix = the staged
  conversion below. ASM v27 ordering for the 5 books without vNN
  filenames came from line knowledge — verify/drag-reorder if needed.
- The ASM Epic v2 duplicate folder was deleted by the user (its 16 rows
  cleaned via rescan).
- Container was on v1.1.18-pending at session end — the v1.1.18 image
  is built on GHCR; user pulls it next session if not already done.

## NEXT ACTIONS (in order)

1. **Verify container is on v1.1.18**, then run the staged conversion:
   POST command `ConvertComicFiles` with the series ids of all
   Fantastic Four Epic + Iron Man Epic series **plus Suske en Wiske**
   (query /api/v1/series by name). Expect ~25 repacks + 5 renames +
   embedding. Then: Kavita rescans (auto via retag notifications; give
   it a scan cycle), POST /readinglist/5/push and /6/push, and a
   `RescanFolders {filter: matched}` so the renamed Suske files map.
2. **Auto-convert-on-import setting** (top backlog): Media Management
   toggle that runs ConvertToRealCbz on freshly imported files before
   the WriteTags call — otherwise every future .cbr grab recreates the
   untagged-in-Kavita drift. Off by default.
3. **Review-modal triage** of the remaining ~143 unmapped (long tail:
   per-file oddities — missing CV issues, one-shot volumes not in
   library, .pdf issues, fractional numbers).
4. Standing watch: quality "Unknown" in Activity manual-import modal;
   Backup task lastDuration renders garbage (cosmetic).
5. User homework unchanged: docker healthcheck on the /comics sentinel;
   create the wiki's first page so docs/wiki/ can push.

## Notes for the next session

- Homelab API key: user pastes per session, as always.
- Dev instance: **indexers + download clients are DISABLED in the dev
  DB** (live-infra footgun removed 07-14); re-enable deliberately if a
  test needs one, then re-disable.
- git identity is repo-local `apoxx9 <noreply>` — pre-push scans are
  fully clean now.
- Suite: 2,560 green (Core.Test only, as always).
- The manual-import API (`GET /api/v1/manualimport?folder=…`) plus the
  live trace toggle (config/host logLevel, restore after!) was the
  killer diagnosis combo this session — start there for any future
  "files won't map" mystery.
