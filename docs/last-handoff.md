# Last Handoff — 2026-07-06 (End of Session 20, updated post-deploy)

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

## Next actions (suggested order)

1. On the homelab: set a real StagingFolder + mount the staging source
   into the container, then use Import from Staging for real.
2. Backlog: weekly pull list (feature-landscape #1, the big Mylar-parity
   item — needs design discussion), terminology key-debt (130 keys +
   RTorrentSettings Music* fossils + panelarr.com test-domain fossil
   failing 3 Common.Test HTTP tests offline).
3. Standing watch: quality "Unknown" in the Activity manual-import modal
   (unreproduced since Session 18); queue-removal/import race (Session
   19 quirk #1, benign).

## Reading list

docs/staging-folder-import.md (now feature-complete state),
docs/publisher-ui.md (2B shipped), docs/feature-landscape.md (#10 #11
shipped).
