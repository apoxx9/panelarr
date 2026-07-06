# Panelarr — Next Session Prompt

Copy this into the first message of a new Claude session (fill in the
API key):

---

We're continuing work on Panelarr (~/Projects/panelarr). Read
`docs/last-handoff.md` first — it has full Session 20 state. Short
version: **v1.1.7 is tagged and on ghcr but NOT yet verified on the
homelab** (staging-folder import feature-complete: settings field,
Import from Staging modal, per-file report, equal-quality-duplicate
guard). After the tag, two more user-visible features landed on main:
**Publisher browsing** (Library → Publishers card grid → filtered
index) and **related-series links** (typed directional SeriesRelations,
migration 12, chip strip on series details). Suite is 2447/0
(Core.Test — that's the metric; Common/Integration failures are
environmental), all pushed.

Homelab: <paste address here>, API key: <paste key here>. It runs
`ghcr.io/apoxx9/panelarr:latest` — last confirmed 1.1.6.82; v1.1.7
may already be live if watchtower pulled it.

Today's agenda, in order:

1. **Verify v1.1.7 on the homelab** (or deploy it first): staging
   import end-to-end with a real staging folder (seedbox mount
   dropoff was the design intent), publisher normalization, and that
   migration 12 applied cleanly.
2. **Release decision**: tag v1.1.8 for publisher browsing +
   related-series once homelab-verified.
3. On the homelab, link the 3 MMPR annuals to MMPR (2016) with the new
   related-series feature (type: annual) — that was the point of
   feature-landscape #11.
4. If time, pick from backlog: **weekly pull list** (feature-landscape
   #1, the big Mylar-parity item — discuss design first), terminology
   key-debt (130 keys + RTorrentSettings Music* fossils + the
   panelarr.com test-domain fossil that fails 3 Common.Test HTTP tests
   offline).

Dev notes: local dev instance data dir is
`~/Library/Application Support/Panelarr` (port 8787; the
`~/.config/Panelarr` copy is stale — wrong API key). The dev DB has
live indexers configured, so stop the instance when done testing.
Puppeteer is installed globally.

Standing watch: quality "Unknown" in the Activity manual-import modal
(unreproduced since Session 18) — if it appears: screenshot, keep the
modal open, query `/api/v1/manualimport?downloadId=` live.

Standing rules as always: test before presenting, discuss designs
before implementing, scan before any push, no AI attribution in
commits, ask before pushing.
