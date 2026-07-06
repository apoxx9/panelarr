# Last Handoff — 2026-07-06 (End of Session 20)

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

## Next actions (suggested order)

1. **Deploy v1.1.7 to the homelab and live-verify staging import there**
   (needs homelab API key / user's deploy flow). Set a real staging
   folder on the homelab (design intent: seedbox mount dropoff).
2. **Release decision**: publisher browsing + related-series are
   user-visible and sit after the v1.1.7 tag — consider tagging v1.1.8
   once homelab-verified.
3. Backlog: weekly pull list (feature-landscape #1, the big Mylar-parity
   item — needs design discussion), terminology key-debt (130 keys +
   RTorrentSettings Music* fossils + panelarr.com test-domain fossil
   failing 3 Common.Test HTTP tests offline).
4. Standing watch: quality "Unknown" in the Activity manual-import modal
   (unreproduced since Session 18); queue-removal/import race (Session
   19 quirk #1, benign).

## Reading list

docs/staging-folder-import.md (now feature-complete state),
docs/publisher-ui.md (2B shipped), docs/feature-landscape.md (#10 #11
shipped).
