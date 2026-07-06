# Staging-folder import (Phase A shipped 2026-07-06, Phase B pending)

Proposed 2026-07-03 during the homelab migration. Design settled and
Phase A (backend) implemented in the 2026-07-06 session (f3dd568 +
f65eb45, verified live end-to-end on the local dev instance: staged
tagged cbz → proposal via cvinfo → StagingImport command → file moved
and renamed under the existing publisher folder). Phase B (frontend)
pending.

## The ask

Point Panelarr at a folder *outside* the library (a staging/dropoff
folder), have it identify the series inside — same proposal engine the
Library Import uses today — and **transfer** the files into the real
comics root on import, organized and named by Panelarr.

Today's Library Import is deliberately in-place: it adopts folders where
they already live inside a root folder and never touches files. That is
right for a Mylar migration, but it means there is no "here's a pile of
comics, file them into my library" flow. Kapowarr's import wizard has an
import-and-rename mode; Mylar has post-processing folders. We have the
identification half already built.

## Settled design (2026-07-06)

1. **Move vs copy: move by default, copy as an option.** Transfer is
   copy → verify → delete-source per file (`DiskTransferService`
   mechanics, as the download pipeline uses). A "keep source files"
   checkbox switches to copy-only, for staging out of folders the user
   doesn't own (e.g. a still-seeding torrent directory on the seedbox
   mount).
2. **Rename on the way in — by honoring the existing media-management
   naming settings**, exactly like the download import pipeline. The
   migration flow's "files untouched" guarantee stays with the in-place
   import; the two flows remain clearly distinct UI actions.
3. **Collisions: reuse the per-file import decision logic** from the
   download pipeline. Existing series → import into it; duplicate issue
   at equal quality → rejected (not an upgrade), file stays in staging;
   higher quality → upgrade replaces. No second collision policy.
4. **Partial failures: per-file transactionality, no series-level
   rollback.** A mid-series failure leaves transferred files imported
   and the rest untouched in staging; re-running the import resumes.
   Rejected/leftover files are listed in the command's report.
5. **Folder scope: both.** A Staging Folder setting in Media Management
   *and* an ad-hoc folder picker on the import page; the configured
   folder is the picker's default. The setting is the hook for later
   Mylar-style automation (watch/scheduled scan) — automation itself is
   out of scope for v1.
6. **Folder-granular, not per-file.** Series-level proposals as in
   Library Import; per-file stragglers go through the existing
   unmapped-files manual import afterwards.
7. **Publisher-folder normalization is part of this feature** (found
   live 2026-07-06: adding series via API created `Boom! Studios`
   alongside the migrated `Boom Studios`). When building a new series
   path, match an existing publisher folder case- and
   punctuation-insensitively before creating a new one. This protects
   every series-add path, not just staging import.

## What already exists to build on

- `LibraryImportProposalService` — folder identification (cvinfo /
  tagged ids / name search) is root-folder-relative but not inherently
  so; the scanner walks any path it is given.
- The download-client import pipeline already does move/copy from a
  foreign folder into the library with renaming, so the file-transfer
  mechanics (including cross-filesystem copy+delete) exist.
- `LibraryImportCommand` accepts `foreignSeriesId + folder` pairs; a
  staging variant adds a target root folder and a transfer step
  between series creation and rescan.

## Implementation sketch

Settings gains an optional Staging Folder. The Library Import page grows
a second action — "Import from staging" — that runs the proposal scan
against the staging (or picked) folder, shows the same review table plus
a target root folder + naming preview and the "keep source files"
checkbox, and queues a command that: creates the series (publisher
folder normalized against existing folders), transfers files per the
decision engine (copy, verify, then delete source unless keep-source),
then rescans the series folders. Unmonitored/monitored choice as today.

Suggested phasing:

- **Phase A (backend):** publisher-folder normalization; staging scan
  endpoint (proposal service over an arbitrary folder); staging import
  command (series create → per-file decision+transfer → rescan) with
  report. Testable end-to-end via API.
- **Phase B (frontend):** settings field, import-page action, review
  table variant, progress/report surface.
