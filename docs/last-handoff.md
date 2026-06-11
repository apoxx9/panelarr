# Last Handoff — 2026-06-11 (End of Session 15)

## Session 15 addendum: GitHub update check (decision #1 — DONE)

`UpdatePackageProvider` is now backed by the GitHub releases API (`apoxx9/panelarr`, 30-min cached via `ICachedHttpResponseService`): stable releases map to update packages (tag → version, body markdown bullets → New/Fixed changes via heading detection, platform asset → FileName/Url when one exists). **Check-only**: the Updates page shows a "download it from GitHub" banner (or the Docker message) instead of an Install button, and `InstallUpdateService` bails cleanly when a release has no packaged asset — so a future packaged release can opt back into in-app updating just by attaching `Panelarr.<ver>.<os>-*.tar.gz/.zip` assets. The `UpdateCheck` health check now works (banner on new releases). The dead `panelarr.servarr.com/v1` Services endpoint is gone: ProxyCheck pings `api.github.com`, ServerSideNotificationService is a no-op, SystemTimeCheck ctor cleaned, `PanelarrCloudRequestBuilder` keeps only the reserved Metadata factory. **Releases must be tagged `vX.Y.Z` with X.Y.Z above the running assembly version** (current builds are 1.0.0.x, so the first release tag should be ≥ v1.0.1). Verified live: `/api/v1/update` returns 200 `[]` (no releases yet) and the **UI sweep is now fully clean** — the last known red mark is gone. New fixture: `UpdatePackageProviderFixture` (canned GitHub JSON: mapping, draft/prerelease/unparsable-tag skipping, latest-version selection, changes parsing, unreachable-GitHub fallback). Core suite: 2,330 passed, 0 failed.

## Session 15 Summary

Deep audit round 3 on the seven never-audited layers (SignalR, refresh/import pipeline, quality engine, database layer, metadata mappers, backup/restore, setup wizard) using five parallel audit agents plus live testing. ~30 findings, ~25 confirmed and fixed; agent accuracy was high this round (one false alarm of note, which inverted into a different real bug). All Priority 2 cleanup completed. 8 commits, all local, **not pushed**.

## Live tests (all PASS after fixes)

- **Backup/restore round-trip**: backup → marker tag added → restore → restart → tag rolled back, library byte-identical (2 series / 35 issues / 9 files / profiles / root folders).
- **Setup wizard on wiped AppData**: full first-run walkthrough; after fixes the CV key persists, summary is honest, Skip works (see below).
- **SignalR live**: websocket frames captured during RefreshSeries — every broadcast has a matching handler; second-tab updates work without reload.
- **Upgrade decisions live** (manual-import evaluation API): CBR over existing CBZ rejected "Not an upgrade"; CB7 (higher rung) accepted; same-size copy rejected by same-file check.
- **Housekeeping**: runs with zero errors (previously failed every run — see D1).
- **No-change refresh is now a no-op**: previously every refresh re-saved and re-broadcast *every issue* forever (see the date bug below).

## Fixed this session (by layer)

### Database (commit 73f2004)
- **CleanupOrphanedSeriesIssueLinks** joined `Issues` on nonexistent `SeriesGroupLink.IssueId` → "no such column" on every housekeeping run (verified live, then fixed to join SeriesMetadata; gained its missing test fixture).
- `SeriesGroupLink.Issue` HasOne mapping joined Issue PK against SeriesMetadataId — removed (property is in-memory only).
- `SeriesMetadata.Publisher` lazy-load registered (was dead).
- CleanupUnusedTags now counts `RootFolders.DefaultTags` — root-folder-default tags no longer deleted.

### Metadata providers / refresh (commit 4fd8bff) — biggest batch
- **Metron API contract was wrong in 5 places** (verified against Metron-Project/metron serializers + mokkari, locked by new raw-JSON contract fixture `MetronResourceContractFixture`): issue list titles come under `"issue"` not `"issue_name"`; detail titles under `"title"`/`"name"` (story-title array!); page count under `"page"`; series list has **no publisher field**; status strings are Cancelled/**Completed**/Hiatus/Ongoing; series types are "Single Issue"/"Limited Series"/"Annual Series"/"Digital Chapters"/… — old switch matched almost none.
- **Issue numbers survived as strings**: ProviderIssue.IssueNumber was `int?` → "0.5"/"1a" became "0", "003" lost padding. Now string with shared `IssueNumberNormalizer` ("003"→"3", "0.50"→"0.5", "1a" untouched).
- **Cover dates pinned to UTC** — they parsed as local midnight, shifted on the DB round-trip, and made *every* issue compare as changed on *every* refresh → permanent re-save + SignalR broadcast flood. `SortOrder` (dead field, populated only by migration 009) also participated in equality and is now set by the mapper and merged.
- Metron publishers were stored as "Unknown" (PublisherName never mapped); VolumeNumber was assigned the CV issue count; Metron covers now populate from the list endpoint.
- **Unconfigured ComicVine now throws a clear IssueInfoException instead of returning null** (null = "not found" = deletes file-less series on refresh — the audit predicted deletion; live testing showed exception-wrapping accidentally prevented it, but the not-found pass-throughs in IssueInfoProxy were dead, so genuine provider 404 handling never worked; both ends fixed).
- Scheduled RefreshSeries refreshed *nothing* when LastExecutionTime was null or >14 days old (`updatedSeries` initialized to empty set instead of null).
- RefreshIssueService NRE guards; Metron rate limiter day-bucket actually enforces + minute-window race fixed; composite search skips unconfigured Metron and falls back to CV on Metron errors; CV HTML stripping decodes entities and runs on search path too; Publisher CleanName same algorithm everywhere; Issue.UseMetadataFrom keeps detail-sourced Overview/CoverArtUrl/PageCount when list-mapped remote is blank (and PrepareExistingChild backfills pre-compare).

### Quality engine / import identification (commit 232b5ed)
- **Comic custom-format conditions were dead** (PageCount, ComicImageResolution — CustomFormatInput comic fields never populated). Now fed from inspected ComicFiles (0 = uninspected = unknown); never-populated ComicFile/ComicSource input fields deleted.
- **AggregateQuality**: folder/client titles parsing to non-null Unknown masked the extension fallback → downloads imported as Unknown quality (re-download loop risk). Now prefers first known quality.
- **DistanceCalculator compared the file's publisher tag against the series name** (copy-paste bug — guaranteed penalty pushing ComicInfo-tagged files over the 0.20 rejection threshold; this plus the no-fallback gap was the Saga 003 mystery, now root-caused in AUDIT.md 11.6). Also normalized issue numbers in distance scoring ("003" vs "3" no longer hard-rejects).
- Import UpgradeSpecification NRE on null DB quality; UpgradeDiskSpecification accepted whole release on null file (now skips) + unused ICacheManager removed.

### Setup wizard + SignalR frontend (commit 574a8a4)
- Metadata credentials were **silently discarded unless a Test button was clicked**; wizard now saves on Next, advances only on success (verified live).
- Completion summary showed ✓ for everything; now indexer/download-client steps report actual configured state, skipped steps show "Skipped".
- Skip button now renders on optional steps (showSkip was computed but unused).
- SignalR `rootfolder` created/deleted broadcasts now refetch the list (were dropped; fetchRootFolders wiring was dead).

### Cleanup (commits 6063e43, 5c6fc35, 4f668ce)
- **ParsedTrackInfo → ParsedFileTagInfo**, TrackNumbers → PartNumbers, LocalIssue.FileTrackInfo → FileTagInfo (API JSON only changes the unconsumed trackNumbers key).
- **All frontend lint errors fixed** (135 pre-existing, surfaced by eslint plugin drift: 104 import-order autofixes + unused vars/imports/propTypes across 15 files). `yarn lint` is clean again.
- **Test fixture migration**: 68 test files moved from music/TV data to comic data (agent-executed; inputs+expectations translated together; feed samples / identification JSON corpus / fuzzy-match negatives deliberately left). Hadouken default category "panelarr-music" → "panelarr".
- SeriesGroup Readarr stubs (Numbered/WorkCount/PrimaryWorkCount): already gone — stale handoff item.

## Audit findings NOT fixed (deferred / design)

1. ~~B3~~ **DONE in session 15 addendum** — was: music-era track grouping can fuse two variants of one issue in a single scan into one "edition"; only one imports, the other silently skips ("already imported"). Needs design: comics should probably never group multiple archive files into one item.
2. ~~B4~~ **DONE in session 15 addendum** — was: rejected-but-identified files are persisted with live IssueId and excluded from future Matched rescans until mtime changes. Touching DiskScanService persistence semantics; design first.
3. **Quality ladder questions**: CB7 (45) ranks above CBZ (40) — upgrades to a worse-supported container; CBZ_Web/CBZ_HD unreachable (no real-world tokens map to them); default profile allows Unknown; parser assumes CBZ for unknown-quality search results (Parser.cs:343) → same release shows different quality in search vs RSS. All one "quality model rethink" discussion.
4. **Q4 — no source-tag quality detection**: "(Digital)", "c2c", "(Scan)", "(f)"/"(Fixed)" aren't parsed into anything. Related to #3.
5. ~~A2~~ **DONE** — refresh now propagates provider publisher renames (also repairs old 'Unknown' placeholders) and a CleanupOrphanedPublishers housekeeper removes unreferenced provider-created publishers (user-created ones are kept).
6. **A4** — duplicate ForeignIssueId across metadata rows aborts series refresh via SingleOrDefault (fragility, needs repair path).
7. ~~D3~~ **DONE** — migration 011 adds the AdditionalInfo column; the class is an embedded document now.
8. ~~F12~~ **DONE** — SearchForNewIssue fetches the full (cached) issue list of the best series match with best-title-first ordering; import-list items without foreign ids can map again, as can remote candidate lookup.
9. ~~F14~~ **DONE** — cache keys redact api keys (no plaintext secret in cache.db, no orphan rows on key rotation) and CV pagination pins limit=100 explicitly.
10. ~~S1~~ **DONE** — orphan handler removed.
11. **Metron live verification**: dev-instance Metron credentials are invalid placeholders — the contract fixes are locked to the serializer source, but a real-credential smoke test would be good (add real creds and add/refresh one Metron series).

## Decisions made (end of Session 15)

1. **Update mechanism**: implement a GitHub-releases update *check* against apoxx9/panelarr (banner + release notes, no in-app auto-install; Docker pulls images). Kills the permanent `/system/updates` 500. The same dead `panelarr.servarr.com` URL backs donations/services — address while in there.
2. **Quality model**: **IMPLEMENTED** (see docs/quality-redesign.md, now updated with verification results and two spec deviations) — source ladder live, migration 010 verified on the dev DB, full suite + all sweeps green (source ladder Scan < C2C < Archive < WebRip < Digital, HD via custom formats, (f)→revision, migration 010 plan, defaults) — parse source tags ((Digital), c2c, (Scan), (Fixed)) into qualities, fix ladder ordering (CB7 vs CBZ, unreachable CBZ_Web/CBZ_HD), remove the search-path CBZ assumption.
3. **Import semantics**: B3 and B4 — **DONE** (commit 54d3dbc, verified live): every comic archive is its own edition (music grouping machinery deleted; quality-ordered import keeps exactly one winner per issue with a visible result for the rest), and files that were not imported are always persisted unmapped so rescans re-evaluate them.

## Test results (final)

- **Core: 2,322 passed, 0 failed, 73 skipped** (+57 new tests: housekeeper fixture, Metron contract + mapper, IssueNumberNormalizer, AggregateQuality, comic conditions, CompositeMetadataProvider guard, DistanceCalculator publisher/number, Issue metadata merge)
- **API: 14 passed, 0 failed**; Common: 618 passed, 3 failed (environmental network downloads — baseline)
- `yarn lint` **clean** (was 135 errors at session start from plugin drift); `yarn build` clean
- smoke-test 36/36; ui-sweep clean except known /system/updates 500; modal-sweep clean (11 flows)

## Uncommitted / Push status

8 commits local (73f2004..4f668ce + this doc). **Not pushed** — scan for sensitive data and ask before pushing, per workflow.

## How to run

```bash
cd panelarr
dotnet build src/Panelarr.sln && yarn build
dotnet run --project src/NzbDrone.Console --framework net10.0
# App at http://localhost:8787
NODE_PATH=$(npm root -g) node tests/smoke-test.js
NODE_PATH=$(npm root -g) node tests/ui-sweep.js
NODE_PATH=$(npm root -g) node tests/modal-sweep.js
```
