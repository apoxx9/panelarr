# Panelarr — Next Session Prompt

Copy this into the first message of a new Claude session (fill in the
API key):

---

We're continuing work on Panelarr (~/Projects/panelarr). Read
`docs/last-handoff.md` first — it has full Session 20 state. Short
version: **v1.1.8 is released, deployed, and verified on the homelab**
(staging-folder import feature-complete with per-file report +
equal-quality-duplicate guard; publisher browsing; related-series
links with migration 12). The 3 MMPR annuals are linked to MMPR (2016)
as type annual on the homelab. Later the same session: **weekly pull
list shipped** (Calendar → Pull List, NCBD weeks, statuses + search),
panelarr.com test fossils fixed, Metadata Provider label fixed. Suite
is 2447/0 (Core.Test; the 3 known Common.Test fossils now pass too).
Note: the docker workflow only updates `:latest` on v* tags; homelab
pulls are manual.

Homelab: <paste address here>, API key: <paste key here>. It runs
`ghcr.io/apoxx9/panelarr:latest` — last confirmed 1.1.8.87.

Today's agenda, in order:

1. **Release decision**: pull list is user-visible — tag v1.1.9?
2. **Real staging-import setup on the homelab**: the container only
   mounts /comics and a local /downloads — add the actual staging
   source (seedbox mount) as a volume, set the StagingFolder setting,
   and run a real Import from Staging.
3. Pick from backlog: story arcs / CBL import (feature-landscape #2+#3,
   the next big Mylar-parity items — discuss design first), Music*
   provider-settings rename (needs a settings-JSON migration — scoped,
   don't sweep), 130 unused translation keys (bulk-safe, needs usage
   scan), Issueshelf rename (decide what the feature is first).

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
