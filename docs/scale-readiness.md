# Scale-readiness review (Session 15 overnight, research only)

Target scale: the post-Mylar-migration library — roughly 300–1000 series,
20,000–60,000 issues/files, ComicVine as primary provider at a hard
200 requests/hour. Nothing here is fixed yet; this is the prioritized map.

Provenance: code sweep by a research agent, with the highest-impact claims
re-verified by hand tonight (marked ✓). Unmarked findings come from the
sweep with file:line evidence and should get a quick spot-check before
being acted on.

## P1 — correctness at scale (do these first)

### 1. ✓ Scheduled refresh likely never refreshes ComicVine series (latent bug, found tonight)

`RefreshSeriesService.Execute` (src/NzbDrone.Core/Issues/Services/RefreshSeriesService.cs:419-431):
when the task last ran within 14 days — which is always true for a daily
scheduled task — it asks the provider for a changed-series delta and only
refreshes series whose ForeignSeriesId is in that set. But
`CompositeMetadataProvider.GetChangedSeries`
(src/NzbDrone.Core/MetadataSource/CompositeMetadataProvider.cs:79-82)
**delegates to Metron only**, even though ComicVineProvider implements
GetChangedSeries too. A migrated library is entirely `cv:`-sourced (the
tagged-import spec records this deliberately), so under the delta path
**no series would ever match and the nightly refresh becomes a no-op for
the whole library** — new issues of ongoing series silently stop
appearing. Manual refresh still works (it bypasses the delta) but costs
one CV call per series: 500 series ≈ 2.5 h against the 200/hr limit.

Morning verification before fixing: confirm what id format Metron's
GetChangedSeries returns and whether ComicVine's implementation is usable
(CV has no clean changed-since API — it may be a date-filtered volumes
query). Likely fix direction: composite delta = union of both providers,
or per-series routing like GetSeriesInfo does.

### 2. Issues endpoint loads the entire table before paginating

`IssueController.GetIssues` (src/Panelarr.Api.V1/Issues/IssueController.cs:60-86):
with no seriesId filter it calls `GetAllIssues()` — all ~50k rows into
memory, materializes Series per row, and only then applies Skip/Take.
Pagination params don't reduce the load; they only shrink the response.
Direction: push page/pageSize into the repository query (LIMIT/OFFSET).

### 3. N+1 in ComicFileController for issueIds batches

src/Panelarr.Api.V1/ComicFiles/ComicFileController.cs:95-103: one
`GetIssue` + one `GetSeries` + one `GetFilesByIssue` **per id** — a
100-issue bulk operation issues ~300 queries. Low-effort fix: batch all
three lookups and join in memory.

## P2 — degraded experience during/after migration

### 4. Per-file SignalR events during bulk import

`ImportApprovedIssues` publishes one ComicFileImportedEvent per imported
file (src/NzbDrone.Core/MediaFiles/IssueImport/ImportApprovedIssues.cs:276-279).
Importing a 50k-file library streams tens of thousands of websocket
messages to every open browser tab and each triggers Redux work. The
migration itself runs series-by-series (31–100 files at a time), which
softens this, but a full-library rescan won't. Direction: batch/progress
events instead of per-file broadcasts.

### 5. Frontend holds the full library client-side

The issue index and unmapped-files table load the complete collection
into Redux and filter/sort client-side
(frontend/src/Issue/Index/IssueIndexConnector.js:17-43,
frontend/src/UnmappedFiles/UnmappedFilesTableConnector.js:18-82). At 50k
issues that's a ~25 MB initial payload, tens of MB of store, and full-list
selector recomputation on every SignalR update. This is the inherited
*arr architecture (Readarr behaves the same), so it "works", but expect
sluggish filtering and slow first paint at the top of the range.
Direction if it hurts in practice: server pagination (#2 is the
prerequisite) + virtualized tables. Suggest measuring after migration
before investing.

### 6. Series endpoint: full list, but probably fine

`SeriesController.AllSeries` returns everything (~2 MB at 500 series) with
batched statistics/next-issue lookups (verified batched by the sweep —
no N+1). Slow-ish first load, acceptable. Lowest priority.

## P3 — the new library-import surface (self-review of tonight's code)

### 7. ✓ Proposal endpoint is synchronous and can be slow without cvinfo

Verified control flow in LibraryImportProposalService.BuildProposal
(src/NzbDrone.Core/MediaFiles/LibraryImport/LibraryImportProposalService.cs:95-159):

- **cvinfo folder (the Mylar case): zero provider calls** — 3 tag reads +
  one cvinfo read per folder. A 500-folder Mylar migration makes 0 CV
  calls at proposal time. The intended scenario is safe.
- Non-cvinfo tagged folder: `ReadSampleTags` reads up to 3 files and
  `ReadMajorityTaggedId` reads the same files again (✓ confirmed
  double-read; cheap — zip central directory + ComicInfo.xml entry, not
  the whole archive — but trivially cacheable), then 1 GetIssueInfo call.
- Untagged folder: 1 search call. **Worst case ~500 provider calls inside
  a synchronous GET** → rate limiter blocks → HTTP timeout. Not our
  migration, but a real failure mode for a general user.

Directions, smallest first: reuse the sampled tags for the majority vote
(one-line cache); cap provider calls per proposal request and mark
overflow folders "needs deeper scan"; longer term, make proposal building
a command with SignalR progress like everything else.

### 8. MetadataProfileId accepted but unused

LibraryImportCommand carries MetadataProfileId; LibraryImportService never
assigns it, so imported series get the model default. Parked in the
handoff as a design question (root-folder default vs. review-screen
control), not a scale issue — listed here for completeness.

## Noted and dismissed

- ComicFiles.IssueId is indexed from the initial schema; unmapped-files
  queries are index scans. No index work needed now.
- Queue endpoint sorts in memory but queues stay small. No action.
- History `/since` endpoint is unbounded but rarely hit; add a limit
  param whenever the file is next touched.

## Suggested sequence

1. #1 delta-refresh bug (correctness; verify then fix) — before the real
   migration matters.
2. #3 N+1 batch fix (small, mechanical).
3. #2 DB-level pagination (unblocks #5 later).
4. #7 small parts (tag-read cache, call cap) opportunistically.
5. #4/#5 only with post-migration measurements in hand.
