# Last Handoff — 2026-07-03 (End of Session 16)

Session 16 spanned: morning review + push of the overnight Phase 2 work,
the refresh-finding retraction, Publisher UI Tier 1+2A, the migration
rehearsal against a real library slice (found and fixed 4 scan/identification
defects — see docs/migration-rehearsal.md), releases v1.1.0 and v1.1.1,
homelab wizard fixes from live setup feedback, and remote verification of
the homelab deployment.

## Where things stand

- **Homelab (Docker, auth enabled; address/API key — ask the user)** is deployed
  on a v1.1.x image and verified remotely: health clean, `/comics` NFS
  mount accessible, initial scan done — **472 unmapped files, 0 series**.
  Proposal triage: **60/60 folders exact via cvinfo, 472/472 files covered,
  0.3 s, zero provider calls.** The migration is review-and-click away.
  GetComics indexer + client configured. Remote smoke: 25/26 (the one
  failure is the browser test hitting the homelab login wall, not a bug).
  API key is NOT in this doc — ask the user.
- **Dev instance** still has the 19 GB rehearsal library imported
  (22 series) at ~/Downloads/Comics; snapshots in /tmp/panelarr-rehearsal/.
- **Local commits NOT yet pushed:** `43d16e4` (batch comicfile lookups,
  series-delete unlinks kept files, Library Import page title) and
  `3b45cda` (GetComics wizard card requires explicit folder + honest copy,
  download-client folder-overlap health check). Candidates for **v1.1.2**.
  Suite at these commits: 2392 passed / 0 failed; smoke 36/36 locally.

## Next actions (in order)

1. **The migration itself** (user-driven, UI): Library Import → Scan for
   New Series → review 60 rows → import (recommend unmonitored first,
   optionally a publisher-slice to start). Then verify: file-mapping
   counts per series, paths point at /comics/..., files untouched.
2. **GetComics end-to-end cycle** on the homelab (grab → download →
   import). This is the one flow never tested through the rehearsal's
   identification changes (remote-candidate legs now skipped when
   AddNewSeries=false) — watch the first real import closely.
3. Push the two pending commits; cut **v1.1.2** when convenient.
4. Backlog: CI version stamping is broken (release images report
   1.0.0.<build> — the tag version never reaches the assembly); the
   smoke-test browser checks can't log in against auth-enabled instances;
   issues-endpoint DB-level pagination (last scale-readiness P1);
   Publisher UI Tier 2B; then the calendar → weekly pull list track
   (feature-landscape #1) once the library is migrated.

## Parked decisions still open

- MetadataProfileId accepted but unused by LibraryImportCommand.
- "Pull List" label on the Issueshelf feature vs. the real pull-list
  feature name collision (terminology audit §2.2).
- Terminology key-debt cleanup (130 legacy localization key names).

## Reading list for context

docs/migration-rehearsal.md (rehearsal results + fixes),
docs/tagged-library-import.md (Phase 1+2 spec, shipped),
docs/scale-readiness.md (P1s remaining), docs/publisher-ui.md (Tier 2B),
docs/feature-landscape.md (roadmap), docs/overnight-handoff.md +
docs/terminology-ux-audit.md (Session 15 research).
