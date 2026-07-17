# Last Handoff — 2026-07-17 (Session 24, final)

Session 24 spanned 07-15 → 07-17 and shipped THREE releases
(v1.1.19 … v1.1.21). The Epic Collections arc is **COMPLETE**: all
three curated reading lists (ASM 22/22, FF 14/14, Iron Man 11/11) are
pushed and live in Kavita with per-volume series. **Unmapped went
143 → 67.** A three-bug pileup on the tag-embed path was found and
fixed, one bug per release, each unmasked by the previous fix.

## Releases shipped

- **v1.1.19** — Auto-convert-on-import (Media Management toggle, off by
  default: converts the library copy post-move/pre-WriteTags, so
  seeding sources are never touched and embeds land in real CBZ).
  Identification: distrust issue title + number duplicated across ≥3
  files (tag accidents without embedded ids — observed live: 6 files
  all tagged "Part Two"/#2, only the real #2 carrying the CV id).
  ConvertComicFiles DI fix: the command crashed in production because
  IFileSystem was never registered (AutoAddServices skips System.*
  interfaces; DiskProvider self-constructs). Swapped to IDiskProvider
  and added a ContainerFixture test that resolves EVERY IExecute<>
  with its full dependency chain — run it before shipping any new
  command.
- **v1.1.20** — Embedded ComicInfo/MetronInfo declared utf-16 over
  UTF-8 bytes (XmlWriter over StringBuilder ignores settings.Encoding)
  — strict parsers (Kavita) silently rejected every tag Panelarr ever
  embedded; Panelarr's own string-based reader never noticed. Both
  generators now emit true UTF-8. ComicInfo also gains Volume (series
  year, CV/Mylar convention — Kavita's ComicVine library type groups
  series by Series + Volume).
- **v1.1.21** — ComicInfo Web now carries the ComicVine issue page URL
  (4000-id form) instead of the cover-art uploads URL. Kavita's
  WeblinkParser.GetComicVineId throws IndexOutOfRange on uploads URLs
  and **drops the file from its library entirely** — this was the last
  blocker for the FF/IM reading-list pushes.

## Homelab state at handoff

- Library: 1,353 series, **67 unmapped**, health clean.
- 76 unmapped fixed this session: stale-metadata refreshes (Get Joker
  had zero issues in DB; Detective 2016 lacked #1027; WW 2011 lacked
  #4; Wolverine 2014 lacked #1) + deterministic ManualImport under a
  strict safety criterion (matched series must own the file's folder
  AND filename number must equal the matched issue number — this
  criterion excluded two would-be wrong imports that fuzzy matching
  rated clean). Angel & Faith Season 10 fully repaired, including
  #024 which had been silently mapped to issue #2 for months.
- Epic Collection files: all real CBZ, tagged with valid UTF-8
  ComicInfo (Series/Number/Volume/Year/Web=CV issue page) after the
  final v1.1.21 retag (401 tagged, 0 failed).
- **83 Wikipedia-derived reading lists** (515 slots, 61 resolved)
  imported via CBL from the Marvel Epic Collection Wikipedia page —
  every line incl. Modern Era as separate lists, wiki volume order,
  unowned volumes as unresolved "missing" slots (collecting
  checklists). Naming: "<Line> Epic Collections". The pre-existing
  owned-only lists 4/5/6 were replaced (Kavita's pushed copies
  untouched).

## Kavita ops knowledge (hard-won this session)

- The scanner SKIPS folders whose mtime is unchanged; renames preserve
  file mtimes. Deleted series on unchanged folders are NOT recreated
  by normal scans. force=true on /api/library/scan bypasses all of it.
- scan-folder API calls get debounced/deferred — don't trust a 200.
- Parse exceptions silently DROP files. **The server log zip
  (/api/server/logs) is the ground-truth diagnostic — go there FIRST,
  not after a day of scan roulette.**
- Reading-list push rejection messages truncate to the first 3
  entries — "3 series missing" can mean "all missing".
- ComicVine-type libraries group by folder parse, overridden by
  ComicInfo Series (Volume).

## Backlog (agreed, in priority order)

1. **Push resolved-slots-only** for reading-list push (Kavita rejects
   lists wholesale when any series is missing — blocks pushing the
   Wikipedia checklists and future partial lines).
2. **CBL matcher leniency**: ignore leading "The" when resolving slots
   (7 CV volumes are titled "The Amazing Spider-Man Epic Collection"
   and only matched after data-side name correction).
3. **Export-all reading lists**: zip of per-list CBLs (endpoint +
   button on the index page). Per-list export already exists.
4. **Wire ComicFileRetaggedEvent**: published by NOTHING today, so
   Kavita's OnIssueRetag notify is dead code — discuss trigger UX
   (avoid double-notify on import, which already notifies).
5. Review-modal triage leftovers (user decisions): 47 annual files need
   their CV annual volumes added; 6 Red Hood Outlaw #41-49 belong to
   Red Hood and the Outlaws (2016) — folder/series decision; ~11
   redundant duplicates ("not an upgrade"); oddities without CV
   issues: Suske #00, Radiant Black #25.5, USM #00.5.
6. Standing watch: quality "Unknown" modal (unreproduced), Backup task
   lastDuration cosmetic bug.

## Notes for the next session

- Homelab API keys (Panelarr AND Kavita): user pastes per session.
- ManualImport command payload: **issueId singular** per file
  ("issueIds" fails as "Issue with ID 0 does not exist").
- Homelab Kavita connection: onRename notify is currently disabled.
- renameComics is false on the homelab by user preference; it was
  enabled briefly (with permission) to rename the 25 Epic files.
- Suite: 2,573 green (Core.Test only) + ContainerFixture 8 green.
- Wikipedia extraction + CBL import scripts and the lists 4/5/6 CBL
  backups live in the session scratchpad (ephemeral) — the imported
  lists themselves are in the homelab DB and covered by its backups.
