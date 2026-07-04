# Last Handoff — 2026-07-03 (End of Session 17)

Session 17 was the real Mylar migration on the homelab plus the GetComics
download-path fixes. The library is now live (60 series, 465 files) and
the GetComics single-issue grab path — flagged least-confident last
session — was found broken and fixed, shipped as v1.1.3.

## Where things stand

- **Homelab (Docker, auth enabled; address/API key — ask the user)** was
  on v1.1.2 (`1.0.0.20604`) at session end and **still needs to pull
  `1.1.3`**. It has the full migrated library: **60 series, 465 mapped
  files, all under /comics/, files untouched** (verified against a
  472-path pre-import snapshot — see below). GetComics indexer + client
  configured; health clean.
- **The migration is DONE and verified.** All 60 cvinfo proposals
  imported exact, unmonitored. Verification script (in the session
  scratchpad) confirmed per-series file counts match statistics, every
  path under /comics/, and mapped+unmapped = the exact 472-path
  snapshot (proves nothing moved/renamed). **7 files intentionally
  unmapped:** 4 redundant PDFs in "MMPR: The Return" (each issue also
  present as .cbz, which won the slot) and 3 "MMPR (2016)" Annuals
  (ComicVine models annuals as separate volumes, not issues of the
  parent — the chronic Mylar annuals-merge problem; a real handling
  decision for later).
- **Releases:** v1.1.2 (the two Session 16 commits) and **v1.1.3** (CI
  version-stamping fix + GetComics grab fixes) are both built, pushed,
  and published to GHCR. Nothing is unpushed; working tree clean.
- **CI version stamping is FIXED** (commit d19121e): the docker workflow
  now derives `<tag>.<run#>` and feeds it via a VERSION build arg to
  both dotnet publish calls. v1.1.2 and earlier still self-report
  `1.0.0.x` (built before the fix); **v1.1.3 should be the first image
  to report `1.1.3.x`** — verify this on the homelab after the pull.
- Suite: **2404 passed / 0 failed** at HEAD (3e8e58b). 12 new tests this
  session.

## Next actions (in order)

1. **Pull `1.1.3` on the homelab + restart.** Then verify two things via
   API (assistant can drive both): (a) `/system/status` now reports
   `1.1.3.x`, not `1.0.0.x`; (b) run the **GetComics e2e** — grab →
   download → import. Good targets (missing, with a working datanodes
   mirror): SIKTC #47 (issueId 451) or Minor Arcana #16 (issueId 261,
   series is monitored from last session). Watch the download step: the
   datanodes handler is new and was verified via curl but not yet
   through Panelarr's own HTTP stack.
2. **Rate-limiter UX fixes (proposed v1.1.4) — awaiting user decision.**
   `ComicVineRateLimiter` is 200/hr refilled in a single hourly burst,
   with no logging and no progress hook — it silently stalled the
   migration for ~55 min mid-import and looked hung. Two small
   independent fixes proposed: (a) log a warning when it starts blocking
   ("rate limit reached, resuming at HH:MM"); (b) drip tokens
   continuously (1 per ~18s) instead of an hourly burst, so bulk imports
   crawl visibly instead of freezing. Metron's limiter already uses a
   per-minute window and is fine. User to decide: both / just the log /
   hold.
3. **ruTorrent/rTorrent download testing (user request).** Panelarr has
   an rTorrent client (ruTorrent is its web UI). Untested in the fork;
   needs a live rTorrent instance configured on the homelab. Separate
   protocol from GetComics DirectDownload.
4. Backlog: annuals handling decision (item 1 above); delete the 4
   redundant PDFs in MMPR: The Return; issues-endpoint DB-level
   pagination (last scale-readiness P1); Publisher UI Tier 2B; staging-
   folder import (docs/staging-folder-import.md, new this session — a
   user idea: import+transfer from a staging folder into /comics);
   calendar → weekly pull list track (feature-landscape #1).

## What shipped in v1.1.3 (the GetComics fix)

Two defects made every real single-issue grab fail:
- `SingleIssueSearchMatchSpecification` called `IssueTitle.Any()` to
  detect full-series packs; comics issues are named by number so
  IssueTitle is routinely null → ArgumentNullException rejected every
  result. Now uses the parser's `IsCollection` flag.
- The download client listed datanodes/vikingfile/fileq/rootz as
  directly-downloadable, but only pixeldrain had a working resolver; the
  rest return HTML to a plain GET → "Site responded with html content".
  Added a real **datanodes** handler (XFS two-step form POST → JSON
  tunnel url, reverse-engineered and verified end-to-end against the
  live site), and dropped the three fake hosts. **vikingfile is
  Cloudflare-Turnstile gated and cannot be automated server-side.**
  fileq (jQuery-XFS) and rootz (Next.js SPA) would each need their own
  handler. Downloads now try each extracted mirror in priority order,
  falling back to the next on failure instead of giving up on the first.

## Parked decisions still open

- MetadataProfileId accepted but unused by LibraryImportCommand.
- Annuals: map to their CV annual volumes as separate series, or add a
  Mylar-style merge-into-parent toggle?
- "Pull List" label on the Issueshelf feature vs. the real pull-list
  feature name collision (terminology audit §2.2).
- Terminology key-debt cleanup (130 legacy localization key names).

## Reading list for context

docs/staging-folder-import.md (new backlog idea), docs/migration-
rehearsal.md, docs/tagged-library-import.md (shipped), docs/scale-
readiness.md (P1s remaining), docs/publisher-ui.md (Tier 2B),
docs/feature-landscape.md (roadmap).
