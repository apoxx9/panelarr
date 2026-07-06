# Last Handoff — 2026-07-06 (Session 20 — marathon; see final addendum)

Session 20 shipped **staging-folder import Phase B** (feature complete),
**released v1.1.7** (CI green, image on ghcr — homelab deploy NOT done,
see below), then cleared two backlog items: **Publisher UI Tier 2B** and
the **related-series link** (feature-landscape #11). Suite: **2447
passed / 0 failed** at HEAD. All work committed on main; d1e7f92 is
tagged v1.1.7, the publisher + relations commits (1169805, 6af0006) are
after the tag.

## Shipped — staging import Phase B (in v1.1.7, d1e7f92)

- **UI**: Staging Folder setting in Media Management → Importing (the
  Importing fieldset is no longer advanced-only — deliberate change);
  Library Import page gained "Import from Staging": folder picker
  defaulting to the setting (PathInput with browser), auto-scan,
  proposal review table, target root folder + quality profile +
  monitored + keep-source-files. Existing series ARE selectable targets
  (unlike in-place import) — filing new issues into a tracked series is
  the main use case.
- **Per-file report**: StagingImportService records every file's
  outcome (imported/rejected/failed + reasons) into an in-memory
  StagingImportReportService keyed by command id;
  `GET /libraryimport/stagingreport?commandId=`. The modal polls the
  command (1.5s) and renders the report table when it finishes — files
  left in staging say why.
- **Equal-quality duplicate fix (important)**: live e2e exposed that the
  shared UpgradeSpecification *accepts* equal quality (download re-grab
  semantics), so a staged duplicate silently REPLACED the library file
  (and the old file was permanently deleted — no recycle bin on dev).
  Violates settled design decision 3. Fixed as a staging-only guard
  (`RejectEqualQualityDuplicates`): equal quality → rejected ("Issue
  already has a file at equal quality"), higher quality still upgrades,
  download pipeline untouched. The dev library file was restored from
  the homelab mount; dev library and settings returned to pre-test
  state exactly.
- Verified with puppeteer e2e: 1 file imported (missing issue), 1
  equal-quality duplicate rejected with reason shown, left in staging.

## Released — v1.1.7

Tagged on d1e7f92, both Docker workflows (main + tag) green, image on
ghcr. **Homelab deploy/verification did NOT happen**: no SSH key auth to
the homelab from this machine and its API key wasn't provided this
session (the next-session prompt placeholder). The homelab still runs
1.1.6.82 unless watchtower pulled :latest.

## Shipped — Publisher UI Tier 2B (1169805)

Library → Publishers sidebar entry: card grid derived client-side from
the loaded series list (initials tile — publisher logo art is empty from
the provider; series/monitored counts). Clicking a card dispatches a
transient 'publisher' filter into seriesIndex state and navigates to the
index — no duplicated series rendering, no new store slice. Known edge:
the transient filter isn't persisted, so a reload with the stale
selected key degrades to "all" (documented in the reducer).

## Shipped — related-series links (6af0006)

Settled as "most future-proof robust": **typed + directional storage,
symmetric rendering**. Migration 12 creates SeriesRelations (SeriesId,
RelatedSeriesId, RelationType: related/annual/spinOff). Service enforces
self-link/missing-series/duplicate-in-either-direction (400s at the
API); cascade-deletes on SeriesDeletedEvent. API /api/v1/seriesrelation.
UI: chip strip above the tabs on series details with inline link form
(series picker + type select), remove per chip. Display-only per the
annuals decision. Verified live both directions + migration on dev DB.

## Quirks / notes

1. Dev instance data dir is `~/Library/Application Support/Panelarr`
   (NOT `~/.config/Panelarr` — that one is a stale April copy with a
   wrong API key). Port 8787.
2. The dev DB carries live indexers/download clients (RSS sync runs and
   can process releases when the dev instance is up) — stop the instance
   after testing.
3. Series API resource uses `seriesName`, not `title`.
4. CV publisher names differ from folders: `Boom! Studios` (metadata) vs
   `Boom Studios` (folder) — expected, cards use metadata names.

## Post-deploy addendum (same day, later in Session 20)

- **v1.1.8 tagged and deployed** (homelab on 1.1.8.87): the docker
  workflow only updates `:latest` on version tags (main pushes only tag
  `:main`), so publisher browsing + related-series needed a tag to
  reach the homelab. No code changes — v1.1.8 = b7f5b17.
- **v1.1.7 staging surface verified on the homelab** (scan works,
  inside-root-folder guard 400s, report 404s for unknown ids,
  stagingFolder setting present). Full move mechanics were dev-verified;
  real homelab staging use needs the staging source (e.g. seedbox
  mount) added as a container volume — only /comics and a local
  /downloads are mounted today.
- **MMPR annuals linked** on the homelab via the new relations API:
  series 20 (MMPR 2016) → 63/64/65 (2016/2017/2018 Annuals), type
  annual; symmetric lookup verified. Feature-landscape #11 is closed
  end-to-end.
- **MMPR Return PDFs "reappeared" — resolved**: the container library
  is clean (cbz only). The 4 PDFs exist only on the /Volumes/data share
  copy, which turns out NOT to be the container's live storage (old
  copy/backup). ~500 MB reclaimable there; user's call.
- Homelab pulls are manual (no watchtower auto-update observed).

## Continued-session addendum (pull list + terminology)

- **Weekly pull list SHIPPED** (f46a9e1, feature-landscape #1): sidebar
  Calendar → Pull List (/pulllist), Wednesday-anchored (NCBD) week
  sections from one week back to four ahead, per-issue status
  (have/grabbed/missing/unreleased/unmonitored), per-issue search +
  week-level 'Search N missing' (IssueSearch), unmonitored toolbar
  toggle. Frontend-only on the existing /calendar endpoint. Puppeteer
  verified on dev (statuses, toggle, command queue, week nav). This
  claims the 'Pull List' name — audit §2.2 collision resolved; the
  Issueshelf still needs a different label.
- **panelarr.com test fossils fixed** (e6fb3ba): the 3 HttpClientFixture
  tests now use repo raw files pinned to a commit SHA. Common.Test
  no longer has known always-fail tests (httpbin reachability aside).
- **'Metadata Provider Source' → 'Metadata Provider'** (audit item 4).
  Audit item 1 (Library Import page title) was already fixed earlier.
- **Music* settings rename SCOPED OUT deliberately**: MusicCategory /
  MusicDirectory etc. span six download clients AND are serialized in
  users' provider-settings JSON — renaming needs a settings migration.
  Own session item, not a sweep. The 130 unused translation keys also
  remain (safe bulk deletion, needs a usage scan).

## Late-session addendum (arcs settled + fossil cleanup)

- **Story-arcs design SETTLED** (docs/story-arcs.md leads with the
  summary): CBL is the interoperability contract (Kavita/Komga is the
  governing requirement), slots model, ids-first matching, CBL export in
  core v1, track-only, CV-only provider v1. Build isolated on a feature
  branch; lossless CBL round-trip is an acceptance criterion. Research
  record (CV/Metron APIs, Mylar, CBL format, Radarr Collections) is in
  the same doc.
- **Pull list opens on This Week** (de693a1) — field finding from the
  first real homelab use; last week collapses behind a link.
- **Issueshelf deleted** (474b32f): unrouted page, unused input type,
  redux slice, FieldType enum member. POST /issueshelf endpoint KEPT —
  it's the live backend of the monitoring-options modal.
- **205 dead translation keys swept** (83d59ab) + 2844 locale orphans;
  scan = quoted literals + dynamic prefixes (PullListStatus_,
  SeriesRelationType_). en.json now 1087 keys.
- **Music*/Tv* download-client settings renamed** (b28462f) to
  Comic*/Issue* across 11 clients, with **migration 13** rewriting the
  stored settings JSON keys. Verified live on the dev DB (configured
  rTorrent kept its category value). NOTE: the homelab needs a release
  tag containing migration 13 before its next pull-from-tag.
- v1.1.9 verified on the homelab earlier (pull list live with real
  data: Minor Arcana #16 missing this week).

## Next actions (suggested order)

1. **Story arcs Phase A** on a feature branch — the settled doc is the
   spec. Next release tag (v1.1.10) should ship the fossil-cleanup batch
   (incl. migration 13) independently of arcs.
2. Release decision: pull list is user-visible — consider v1.1.9 (or
   fold into the next feature release).
2. On the homelab: set a real StagingFolder + mount the staging source
   into the container, then use Import from Staging for real.
3. Backlog: story arcs / CBL import (feature-landscape #2+#3 — design
   discussion first), Music* provider-settings rename (needs a
   settings-JSON migration), 130 unused translation keys (usage scan),
   Issueshelf rename (decide what the feature is first).
4. Standing watch: quality "Unknown" in the Activity manual-import modal
   (unreproduced since Session 18); queue-removal/import race (Session
   19 quirk #1, benign).

## Final addendum — Reading Lists feature (branch: feature/story-arcs)

After the fossil cleanup, the session continued into the READING LISTS
feature (nee story arcs). State at handoff:

- **Design settled + researched** (docs/story-arcs.md — leads with the
  settled summary; research record below it). Key calls: CBL file is the
  interoperability contract (Kavita target), named "Reading Lists" with
  arc as a TYPE, track-only, no auto-add, explicit add-missing-series,
  lossless CBL round-trip as acceptance criterion.
- **Phase A (backend) BUILT on the branch**: migration 14 (ReadingLists
  + ReadingListItems slots incl. ForeignSeriesId), CV story-arc
  search/fetch (membership from arc detail's issues array hydrated in
  id-batches — /issues does NOT support a story_arc filter, it silently
  returns everything; first version crawled to offset 20000 and got
  velocity-blocked), ReadingListService (cover-date ordering, TPB
  filter, id-first + name-fallback resolve, lazy unresolve), CBL
  parse/write with <Database Name=cv> ids, /api/v1/readinglist
  (list/detail/search/add/import/export/addseries/delete). 14 tests.
- **Phase B (frontend) BUILT**: /readinglists index (CV search panel,
  CBL upload, coverage) + /readinglists/:id detail (ordered slots,
  live statuses, search missing, add-missing-series panel, export CBL,
  delete), sidebar Library → Reading Lists. Puppeteer e2e written
  (CBL path, no CV needed).
- **Phase C SCOPED**: Kavita/Komga push is small — both exist as
  notification connections, Kavita proxy already does the JWT dance;
  needs ImportCbl per proxy + push endpoint + button.
- **Verification state**: unit suite green (2461+ incl. reading lists);
  live CV add (Shattered Grid) + the Phase B UI e2e were pending at
  handoff time — check the session end summary/chat for results before
  merging. DO NOT merge feature/story-arcs to main until both pass.
- Dev-DB note: migration 14 was applied/reset during the rename; if the
  dev DB acts odd around ReadingLists tables, drop them + delete
  VersionInfo row 14 and restart.

## Reading list

docs/staging-folder-import.md (now feature-complete state),
docs/publisher-ui.md (2B shipped), docs/feature-landscape.md (#10 #11
shipped).
