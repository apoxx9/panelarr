# Overnight handoff — Session 15 loop (night of 2026-06-12)

Morning index. Everything below is **local only — nothing was pushed.**
8 commits sit on main ahead of origin, ready for your review.

## TL;DR

- **Phase 2 of tagged library import is done and live-verified.** Scan
  button → review modal → exact cvinfo proposal → import lands the series
  at the existing folder, files mapped, archives untouched. Suite 2371/0,
  smoke 36/36, UI/modal sweeps clean, new 15-check UI e2e green.
- **All four Part 2 research docs are written** (links below).
- **One real pre-existing bug found and verified during research:** the
  nightly scheduled refresh silently skips every series — for all
  providers. Details in scale-readiness #1. Not fixed (out of loop
  scope); it's a one-line fix + test once you confirm the direction.

## Commits awaiting your review (oldest first)

1. `5f49ba4` Library import: propose series from a root folder's unmapped files
2. `fbcd195` Library import: bulk-add confirmed proposals as a background command
3. `19d2c0c` Library import: review screen on the Library Import page
4. `078cca6` Mark tagged-import Phase 2 shipped with live verification notes
5. `2b52ddd` Publisher UI design draft for morning review
6. `5735afe` Terminology and UX wording audit (research, no changes)
7. `b3d343a` Scale-readiness review and comic-manager feature landscape (research)
8. (this handoff)

Pre-push reminder: run the usual sensitive-data scan; docs were written
with relative paths but double-check before publishing.

## What shipped (Part 1 — Phase 2)

Spec with verification notes: docs/tagged-library-import.md (Phase 2
section). Implementation:

- Backend: LibraryImportProposalService (+endpoint
  `GET /api/v1/libraryimport/proposal?rootFolderId=`), LibraryImportCommand
  + LibraryImportService (sequential AddSeries at the EXISTING folder,
  no search-on-add, refresh+rescan maps files via Phase 1 exact ids).
- Frontend: "Scan for New Series" button on the Library Import page →
  review modal (exact pre-checked, probable unchecked, already-in-library
  rows disabled; global quality profile + monitored).
- 12 new unit tests; UI e2e at tests/libraryimport-e2e.js (local —
  tests/ is gitignored; header documents how to re-stage).

Live verification evidence: TWD folder, cvinfo → `cv:30345` exact, 31
files, **zero provider calls**; import added series at
`import_test/The Walking Dead (2004)`; archives byte-untouched (mtime
precedes import). Done twice: once via raw API, once through the real UI.

## Part 2 research docs (the reading list)

- **docs/scale-readiness.md** — prioritized; items marked ✓ I re-verified
  by hand in the code, the rest are sweep-reported with file:line.
  Headline: the refresh no-op bug (#1), in-memory pagination on the
  issues endpoint (#2), an N+1 on bulk comicfile lookups (#3).
- **docs/terminology-ux-audit.md** — headline is good news: zero
  user-visible book/music leftovers. 130 legacy localization *key names*
  remain (mechanical cleanup), plus a short real-wording list (page-title
  tension on Library Import, "Pull List" mislabel, two cosmetics).
- **docs/publisher-ui.md** — backend already complete (entity, CRUD API,
  index column); draft proposes tiers: filter → group-by-publisher →
  publisher pages. Publisher logo images verified empty in the live DB —
  logo fetch would be its own small backend increment.
- **docs/feature-landscape.md** — Mylar3/Kapowarr/Komga/Kavita/*arr
  survey; top gaps ranked for a Mylar migrator: weekly pull list, story
  arcs, CBL import, want list/one-offs, comic-aware custom formats.

## Parked design questions (decisions deliberately NOT made)

1. **Refresh no-op bug fix shape** (scale doc #1): providers return null
   vs composite maps empty→null; and what `ShouldRefresh` staleness
   thresholds should be once the path works again (drives daily CV call
   volume at library scale).
2. **MetadataProfileId is accepted by LibraryImportCommand but unused** —
   imported series get the default metadata profile. Add it to the review
   modal's globals, take the root-folder default, or drop the field?
3. **Existing-series-with-different-path** during library import: v1
   disables those rows ("Already in Library"). Should a future version
   offer "move series path to this folder" or "merge"?
4. **Series delete leaves phantom mappings** (observed while staging the
   e2e): `DELETE /series/{id}?deleteFiles=false` keeps ComicFiles rows
   pointing at deleted issue ids; they're invisible to the unmapped view
   until Housekeeping's CleanupOrphanedComicFiles unlinks them. Should
   series-delete unlink immediately (matching user expectation that kept
   files reappear as unmapped right away)?
5. **"Pull List" label** (terminology doc §2.2): the `Issueshelf` feature
   is labeled "Pull List" but isn't one — and a real weekly pull list is
   the feature-landscape #1 candidate. Naming collision to resolve before
   building either.
6. **Publisher UI tier choice** (publisher doc ⚖ marks): grouping toggle
   vs publisher pages vs both; sidebar entry or not; imprints stay flat?
7. **DDL sources** (feature landscape #8): policy call before any
   technical evaluation.

## Dev environment state

- Dev app still running from `_output/net10.0/Panelarr -nobrowser`
  (background task; log at /tmp/panelarr-overnight/dev-phase2.log).
- Library: Power Rangers Prime, Absolute Superman, The Walking Dead
  (re-imported via the UI e2e; series id 8, monitored — the modal's
  default — quality profile 1; delete it if you want a clean slate).
- **Pre-Phase-2 DB snapshot:** /tmp/panelarr-overnight/panelarr-pre-phase2.db
  (+ config backup alongside). Restore = stop app, copy over
  ~/Library/Application Support/Panelarr/panelarr.db, start app.
- Known dev-data caveat (disclosed earlier tonight): the TWD dev copies
  lost their original Mylar ComicInfo.xml to the old embed bug before it
  was fixed; their tags are now Panelarr-written. The folder cvinfo is
  original. Vol. 16 file was consumed by Phase 1 testing.
- import_test/ is otherwise untouched (loop contract).

## Suggested morning order

1. Review the 8 commits (Phase 2 code first), then decide on pushing.
2. Read scale-readiness #1 (refresh no-op) — likely the next fix.
3. Decide parked questions 2-5 (each is small once decided).
4. Pick the next feature from the landscape doc when ready.
