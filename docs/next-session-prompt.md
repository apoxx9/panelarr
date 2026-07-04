# Panelarr — Next Session Prompt

Copy this into the first message of a new Claude session (fill in the
API key):

---

We're continuing work on Panelarr (~/Projects/panelarr). Read
`docs/last-handoff.md` first — it has full Session 18 state. Short
version: v1.1.4 (rate-limiter drip + `:latest` follows release tags)
and v1.1.5 (publisher-folder import fix) are shipped and verified on
the homelab; BOTH download protocols are proven live end-to-end
(GetComics direct download incl. the datanodes handler, and Prowlarr →
seedbox rTorrent → mount → path mapping → import). Suite is 2411/0.

Homelab: <paste address here>, API key: <paste key here>. It runs
`ghcr.io/apoxx9/panelarr:latest`, which now tracks releases — currently
1.1.5.79.

Today's agenda, in order:

1. **Ignored-download cache fix (proposed v1.1.6).** Removing a queue
   item without removing it from the client leaves the tracked download
   cached as Ignored; `TrackedDownloadService.TrackDownload` returns the
   cached item without re-reading download history, so re-grabbing the
   same infohash stays invisible until a restart. Bit us live in
   Session 18 (cross-seeded torrents share one infohash across
   trackers). Discuss the fix shape first, then implement + test.
2. **Annuals handling decision** (3 unmapped "MMPR (2016)" annuals):
   map their CV annual volumes as separate series vs a Mylar-style
   merge-into-parent toggle. Discuss, decide, maybe implement.
3. **Delete the 4 redundant PDFs** in "MMPR: The Return" (each issue
   also present as .cbz which won the file slot).
4. If time, pick from backlog: issues-endpoint DB pagination (last
   scale P1), staging-folder import (docs/staging-folder-import.md),
   Publisher UI Tier 2B, calendar → weekly pull list, terminology
   key-debt (130 keys + the RTorrentSettings Music* naming fossils).

Standing watch: if quality "Unknown" ever reappears in the Activity
manual-import modal, screenshot it and keep the modal open — then query
`/api/v1/manualimport?downloadId=` live to pin frontend vs backend
(Session 18 could not reproduce it; all backend paths return Archive).

Standing rules as always: test before presenting, discuss designs
before implementing, scan before any push, no AI attribution in
commits, ask before pushing.
