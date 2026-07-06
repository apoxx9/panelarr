# Panelarr — Next Session Prompt

Copy this into the first message of a new Claude session (fill in the
API key):

---

We're continuing work on Panelarr (~/Projects/panelarr). Read
`docs/last-handoff.md` first — it has full Session 19 state. Short
version: **v1.1.6 is shipped and live-verified on the homelab**
(ignored-download cache eviction on re-grab, issues-endpoint DB
pagination, rate-limiter test fix); the whole homelab maintenance
agenda is done (3 MMPR annuals mapped as separate series per the
settled annuals decision, redundant PDFs deleted); and **staging-folder
import Phase A (backend) is implemented and live-verified** — proposal
scan over an arbitrary folder, StagingImport command with
move/keep-source semantics, StagingFolder setting, and publisher-folder
normalization (fixes the `Boom! Studios` vs `Boom Studios` split).
Suite is 2433/0 (Core.Test — that's the metric; Common/Integration
failures are environmental), smoke 24/24.

Homelab: <paste address here>, API key: <paste key here>. It runs
`ghcr.io/apoxx9/panelarr:latest` — currently 1.1.6.82.

Today's agenda, in order:

1. **Staging import Phase B (frontend).** Design is settled
   (docs/staging-folder-import.md): Media Management settings field for
   StagingFolder; Library Import page gains an "Import from staging"
   action — folder picker defaulting to the setting, same proposal
   review table plus target root folder + "keep source files" checkbox;
   command progress/report surface. UX note from Phase A testing:
   untagged files can fail the 80% fuzzy match and stay in staging —
   surface per-file rejection reasons. Puppeteer is NOT installed on
   the dev machine (`npm i -g puppeteer`) — install it first so browser
   tests can run; backend e2e can be re-run against the local dev
   instance (see Phase A live-test recipe in the handoff).
2. **Release decision**: tag v1.1.7 when Phase B lands (Phase A+B
   together make the feature user-visible).
3. If time, pick from backlog: Publisher UI Tier 2B, related-series
   link (feature-landscape #11, display-only grouping for the annuals),
   calendar → weekly pull list, terminology key-debt (130 keys +
   RTorrentSettings Music* fossils + the `panelarr.com` test-domain
   fossil that fails 3 Common.Test HTTP tests offline).

Standing watch: quality "Unknown" in the Activity manual-import modal
(unreproduced since Session 18) — if it appears: screenshot, keep the
modal open, query `/api/v1/manualimport?downloadId=` live. Also noted
in Session 19: removing a completed queue item does not cancel an
already-dispatched import (benign race, see handoff quirk #1).

Standing rules as always: test before presenting, discuss designs
before implementing, scan before any push, no AI attribution in
commits, ask before pushing.
