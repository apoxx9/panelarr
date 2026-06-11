# Tagged Library Import — Mylar Migration Path

Design spec, end of Session 15 addendum (2026-06-11), design talk held
2026-06-12 — all decisions below are CONFIRMED with Lorenzo. Target: the next
feature session (ahead of Publisher UI — this unblocks populating the library
at all).

## Decisions (confirmed)

- **In-place import**: a root folder points at the existing Mylar library;
  files map where they sit, zero file operations during migration. Renaming
  later via Organize. (Mylar should stop managing the folder afterward.)
- **Grouping precedence** (verified against Lorenzo's real library — see
  Evidence): folder `cvinfo` volume id → per-file Web/Notes issue ids (folder
  majority) → embedded Series + year-hint name search → filename parse. One
  proposal per folder; files whose issue id disagrees with the folder's series
  are listed as outliers and stay unmapped (not force-fitted).
- **Review screen**: root-folder defaults (profile/monitored/monitor-new)
  applied to all, one global override control at the top; `exact` groups
  pre-checked, `probable` unchecked; stale CV ids fall back to name search
  flagged "stale id".
- **Phase 1 ships first** as its own change, verified against the real Mylar
  files already in import_test/.

## Evidence (from Lorenzo's actual Mylar library, The Walking Dead (2004)/)

- Series folder contains **`cvinfo`**: a single line with the ComicVine
  VOLUME url (`https://comicvine.gamespot.com/the-walking-dead/4050-30345/`)
  → direct volume id per folder, no resolution lookups needed.
- ComicInfo.xml carries Series/Number/Title/Publisher/Year plus
  `Web: https://comicvine.gamespot.com/...4000-338482/` (issue id) and
  `Notes: Tagged with the ninjas.walk.alone fork of ComicTagger ... [CVDB338482]`.
- **Format gotchas the implementation must honor:**
  1. Notes id appears as `[CVDB338482]` — NO separator. Regex:
     `\[CVDB[:\s]*(\d+)\]`, plus the `\[Issue ID (\d+)\]` variant.
  2. Mylar writes the series START YEAR into `<Volume>` (here `2004`).
     Embedded Volume is a year hint, NEVER a volume number.

## Why

The primary migration source is a Mylar-managed library where every file has
embedded ComicInfo.xml written by ComicTagger — including the **exact
ComicVine issue id** in the `Web` field
(`https://comicvine.gamespot.com/.../4000-<issueId>/`) and `Notes`
(`...[Issue ID <id>]`, older `[CVDB:<id>]`). Today Panelarr wastes that:

1. `ComicInfoReaderService.ParseXmlContent` already captures *all* XML fields
   into a dictionary, but `ComicInfoIdentification` surfaces only
   Series/Title/Number/Volume/Year/Publisher — `Web`/`Notes` are parsed and
   dropped (ComicInfoReaderService.cs:77).
2. `ParsedFileTagInfo.ForeignIssueId` exists but is never populated; the
   Lidarr-era "match by tagged id" branches in CandidateService are dead code
   (audit A5). Identification is always fuzzy name/number matching, even when
   the file self-identifies exactly.
3. Import can only map files to series **already in the library**
   (`AddNewSeries = false` everywhere; the Select Series modal lists only
   library series). A perfect identification of a not-yet-added series cannot
   act. Migrating N series means hand-adding all N first.

## Principles

- **Trust the tags for matching, not for committing.** Tags eliminate manual
  matching; a preview/confirm step remains. ComicVine merges/renames volumes;
  a years-old library will contain some stale ids, and bulk-adding hundreds of
  series deserves one human glance, not zero.
- **Bulk add is a background command, not an API storm.** N series = N volume
  fetches + issue pagination against ComicVine's in-process budget (200/hr).
  The command adds series sequentially through the existing rate limiter and
  imports each series' files as it lands; progress over SignalR like any
  command.
- **Provider affinity consequence:** CV-id-driven adds make the migrated
  library `cv:`-sourced; refreshes stay on ComicVine per series. Acceptable —
  note it in docs.

## Phase 1 — exact-id identification (small, ships value alone)

1. `ComicInfoIdentification` gains `Web` and `Notes`; reader surfaces them
   from the existing fields dictionary.
2. `MetadataTagService.ReadComicTags` derives the ComicVine issue id:
   - `Web` matching `comicvine.gamespot.com/.../4000-(\d+)` → id
   - else `Notes` matching `\[(?:Issue ID|CVDB)[: ]*(\d+)\]` (case-insensitive)
   - sets `ParsedFileTagInfo.ForeignIssueId = "cv:" + id`
3. Identification fast path: in `CandidateService.GetDbCandidatesFromTags`,
   when the edition's file carries a ForeignIssueId that matches a library
   issue (`IssueService.FindByForeignId`), return that single candidate and
   skip distance scoring (resurrect the dead tagged-id branch properly).
   Distance scoring still applies when the id is unknown to the library.
4. MetronInfo.xml: same treatment if it carries ids (`GTIN`/`ID` fields) —
   investigate during implementation; CV path is the priority.
5. Tests: reader (Web/Notes variants, both Notes formats, garbage), tag
   service mapping, candidate fast path (id-in-library wins over fuzzy;
   id-not-in-library falls back), end-to-end: scan a tagged file for an
   existing series → maps without distance involvement.

## Phase 2 — "Import existing library" flow

**Backend**

1. New endpoint `GET /api/v1/libraryimport/proposal?rootFolderId=` (command or
   long request — decide by scan size): scans unmapped files, reads tags, and
   groups them into proposed series:
   - Files with CV issue ids: resolve issue → volume id (one
     `GetIssueInfo` per *distinct series guess*, not per file — group first by
     embedded Series+Volume+Year, then confirm the volume id from one
     representative file). Confidence: `exact`.
   - Files with tags but no id: provider search by series name + year.
     Confidence: `probable` (top hit attached, alternatives available).
   - Files with no usable tags: left out of proposals (stay unmapped, current
     manual flow still applies).
2. Proposal model: `{ proposedSeries (foreignId, name, year, publisher,
   issueCount), confidence, files[], existingSeriesId? }` — series already in
   the library short-circuit to "will map to existing".
3. `LibraryImportCommand` accepts the confirmed proposals: per series —
   AddSeries (existing service; monitored/profile defaults from the root
   folder's settings) → refresh populates issues → rescan maps the folder's
   files (Phase 1's exact-id matching makes this precise). Sequential, rate
   limiter respected, one SignalR-visible command with progress messages.

**Frontend**

4. Extend the `/unmapped` (Library Import) page: a "Scan for new series"
   action → proposal review table (series, confidence badge, file count,
   override picker per group reusing the search modal) → Import button queues
   the command. Existing per-file manual import stays for the leftovers.

## Formerly open questions — now settled

1. Monitored/profile defaults: root-folder defaults + one global override on
   the review screen (no per-row editing in v1).
2. `exact` pre-checked, `probable` unchecked.
3. Volume-id resolution: mostly moot — `cvinfo` provides the volume id per
   folder directly; the one-GetIssueInfo-per-series fallback applies only to
   folders without cvinfo.

## Out of scope

- Writing tags back during migration (retag exists already).
- Metron-id-driven bulk import (no Mylar source writes Metron ids today).
- Auto-import without review.
