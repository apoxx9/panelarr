# Last Handoff — 2026-07-04 (End of Session 18)

Session 18 shipped two releases (v1.1.4, v1.1.5), verified CI version
stamping end to end, and proved BOTH download protocols live on the
homelab: GetComics direct download and Prowlarr → seedbox rTorrent.
Suite: **2411 passed / 0 failed** at HEAD.

## Shipped

- **v1.1.4** — (a) ComicVine rate limiter reworked: tokens drip
  continuously (one per ~18s, capped at 200) instead of one hourly
  burst, plus a throttled Warn when blocking starts (the silent
  55-min migration stall is structurally gone; 5 new tests).
  (b) Docker `:latest` now follows release tags (`v*`); main pushes
  get `:main` instead. The homelab can stay on `latest`.
- **v1.1.5** — import failed for the FIRST series of any new
  publisher: ComicFileMovingService/UpgradeMediaFileService assumed
  the series folder's parent is the root folder, but Panelarr's layout
  inserts a publisher folder. Now resolves the configured root via
  RootFolderService (missing-root safety check preserved; 2 new
  regression tests). Found live: the whole migrated library is Boom
  Studios, so the first Dynamite series ever imported tripped it.
- **Version stamping VERIFIED**: homelab reports `1.1.5.79` (was
  stuck at `1.0.0.x` because `:latest` used to be the unstamped main
  branch build — root-caused, not a stamping failure).

## E2E verifications (both on the live homelab)

- **GetComics direct download**: SIKTC #47 (issueId 451) — search →
  grab → datanodes mirror (two-step form POST → tunnel URL, through
  Panelarr's real HTTP stack) → import, ~7s total. The new datanodes
  handler is production-verified.
- **Torrent via Prowlarr + seedbox rTorrent**: Ben 10 (2026) #1
  (seriesId 61, issueId 470) — Torznab search → grab → seedbox
  download into `downloads/Panelarr/` → visible through the homelab's
  seedbox mount → remote path mapping translation → import with
  publisher folder auto-created.

## Homelab topology (secrets NOT here — ask the user for address/API key)

Unraid host. Panelarr container mounts: `/comics` = remote library
share; `/downloads` = a subfolder of the SMB/remote seedbox mount
(container `/downloads` **is** the seedbox's `downloads/Panelarr/`
directory). Prowlarr runs on the same host (port 9696); indexers:
GetComics (direct) + a private tracker via Torznab (categories fixed
to **7030 Comics only** — 7020/8010 caused EPUB/MOBI novel noise).
rTorrent download client = a remote seedbox (XML-RPC over SSL);
client Directory is set to the seedbox's `downloads/Panelarr` so
grabs land inside the mounted folder; remote path mapping translates
seedbox paths → `/downloads/`. GetComics client downloads to
`/downloads/getcomics` (its own subfolder, intended).

## Quirks discovered (candidate fixes, not yet done)

1. **Ignored-download cache short-circuit** (potential small fix):
   removing a queue item without removing from the client marks the
   tracked download Ignored in the in-memory cache;
   `TrackedDownloadService.TrackDownload` then returns the cached item
   without re-reading download history, so re-grabbing the same
   torrent (same infohash — note: cross-seeded torrents on different
   trackers can share ONE infohash) never resurrects it until a
   restart rebuilds the cache. Bit us live; restart was the fix.
2. **Quality "Unknown" in the Activity manual-import modal** — seen
   once by the user, unreproducible afterwards: every backend path
   (folder scan, single-file, with/without client context) returns
   Archive for the file, stored data is clean, and AggregateQuality
   is unchanged since v1.0.1. Likely a frontend placeholder row during
   one of the day's broken states. If it reappears: screenshot, keep
   the modal open, hit `/api/v1/manualimport?downloadId=` live.

## Next actions (suggested order)

1. Quality-Unknown repro watch (item 2 above) — passive.
2. Consider the ignored-download cache fix (item 1 above) for v1.1.6.
3. Annuals handling decision (3 unmapped "MMPR (2016)" annuals):
   separate CV volumes as separate series vs Mylar-style merge toggle.
4. Delete the 4 redundant PDFs in "MMPR: The Return" (each issue also
   present as .cbz).
5. Backlog: issues-endpoint DB pagination (last scale P1), Publisher
   UI Tier 2B, staging-folder import (docs/staging-folder-import.md),
   calendar → weekly pull list, terminology key-debt (130 keys; add
   the RTorrentSettings `MusicCategory`/`MusicDirectory` naming
   fossils to it).

## Parked decisions still open

- MetadataProfileId accepted but unused by LibraryImportCommand.
- Annuals (see above). — "Pull List" naming collision (terminology
  audit §2.2). — Terminology key-debt cleanup.

## Reading list

docs/staging-folder-import.md, docs/scale-readiness.md,
docs/publisher-ui.md, docs/feature-landscape.md,
docs/tagged-library-import.md (shipped; context for import flows).
