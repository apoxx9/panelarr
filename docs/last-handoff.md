# Last Handoff — 2026-07-07 (Session 21)

Session 21 shipped **Reading Lists Phase C** (push to Kavita/Komga),
then **merged feature/story-arcs to main** (fast-forward, 0ee36f7) and
tagged **v1.1.11** — the release that finally carries the whole batch:
fossil cleanup + **migration 13** (Music*/Tv* download-client settings
rename) + **migration 14** + Reading Lists A+B+C. Later the same
session: a field bug from the user's first real CBL import led to
**v1.1.12** (tiered resolve + manual relink + repairing export — see
below), and a second field bug (raw 'StatusEndedContinuing' label on
every series page) led to **v1.1.13**. Suite: **2482 / 0**.

## Shipped — translation-key fix + guard (87c0167..cb9cbbe, v1.1.13)

The Session 20 dead-key sweep's usage scan only covered .js files and
deleted 4 keys still referenced from SeriesStatus.ts — series pages
rendered the raw key as the status badge since v1.1.10. Restored those
plus 2 keys that were never defined (DateAdded, Type). The FULL
translation surface was then audited and cemented as tests in
LocalizationCleanlinessFixture: (1) every frontend translate('Key')
literal exists in en.json (.js/.ts/.tsx), (2) every backend
GetLocalizedString key exists, (3) the dynamic-prefix families
(ReadingListType_/SeriesRelationType_ enum-driven, ReadingListStatus_/
PullListStatus_ literal sets) are complete. No variable-keyed
translate() calls exist. README feature list updated for v1.1.12.
Wiki content drafted in docs/wiki/ (5 pages, local-only) — BLOCKED on
the user creating the wiki's first page in the GitHub UI, then push
the pages to the wiki git repo.

## Shipped — CBL resolve resilience + relink (c428637, v1.1.12)

Field finding (user's homelab import): the DieselTech 'Simplified
Power Rangers 1a' CBL points its Shattered Grid entry at CV volume
120566 / issue 715246 — the 2019 TPB record (the CV issue is literally
named 'TPB') — while the library carries the 2018 one-shot
(113084/682536) under the IDENTICAL series name. CV has duplicate
same-named volumes; community CBLs carry wrong ids. Resolver stopped
at the id miss → owned book showed 'not in library'.

- **Tiered resolve**: id miss now falls through to exact name+number
  (Kavita-style tiering); multiple same-named series disambiguate via
  the slot's Volume/Year hint vs series year; still-ambiguous stays
  unresolved (never guess). New FindAllByName on series repo/service.
- **Manual relink**: PUT /readinglist/{id}/slots/{slotId} {issueId} +
  wrench action per slot row (series+issue pickers). Rewrites the
  slot's stored identity from the chosen issue — user's fix wins,
  durable. Deliberately NO unlink: lazy resolve would re-match on the
  next view, so unlink can never stick; wrong link → relink.
- **Repairing export**: ExportCbl writes the RESOLVED issue's cv id,
  so a list resolved past a bad community id exports corrected (could
  be contributed back to DieselTech).
- Verified live with the real community CBL on dev: slot self-healed
  on view, export carries 113084/682536, relink editor round-trips
  (puppeteer). The user's homelab list self-heals on first view after
  pulling v1.1.12.

## Shipped — Reading Lists Phase C (0ee36f7)

- **Per-connection opt-in**: 'Send Reading Lists' checkbox (default
  OFF) on the Kavita and Komga connection settings. Settled with the
  user: opt-in toggle over push-to-all-configured.
- **POST /api/v1/readinglist/{id}/push**: exports the list via the
  existing ExportCbl, fans out to every opted-in reader connection,
  returns per-connection results (created/updated, matchedCount,
  unmatched entries with reasons, errorMessage on failure — one dead
  connection doesn't break the rest).
- **Kavita proxy PushCbl**: IMPORTANT version fork discovered — Kavita
  v0.9 (stable since 2026-04) DELETED the one-shot
  /api/cbl/import; the flow is now file-import (multipart cblFile) →
  re-validate → finalize-import. Empty decisions still import all
  auto-matched items and a same-name list updates in place (verified in
  Kavita source AND live). Shipped: v0.9 flow with 404-fallback to the
  v0.8 one-shot (?useComicVineMatching=true). Auth = existing plugin
  JWT dance, Bearer header.
- **Komga proxy PushCbl**: match/comicrack (multipart) → accept ONLY
  unambiguous matches (single series candidate), rest reported → upsert
  by name (GET search + PATCH bookIds, else POST; Komga 400s duplicate
  names). Enums come over Kavita's wire as NUMBERS (no string enum
  converter registered) — DTOs use ints.
- **UI**: 'Push to Readers' toolbar button on the reading-list detail
  page + inline per-connection results panel (green/red rows, unmatched
  sub-list, no-connections guidance message). New icons.UPLOAD.
- **Tests**: 12 new (push service fan-out/opt-in/isolation/filename
  sanitize; Kavita v0.9 flow + fallback + bearer + validation-fail;
  Komga create/update/all-unmatched/error-code). Suite 2473/0.

## Verified

- Puppeteer e2e against a **mock Kavita v0.9 server** (python) through
  the real app: authenticate → file-import (Bearer + multipart) →
  re-validate (server-side fileName echoed) → finalize (empty
  decisions) → UI panel 'list created, 3 issues matched'. Failure path
  (mock down) renders red 'Connection refused' row. No-connections
  returns [] and renders the guidance message.
- **LIVE against homelab Kavita 0.9.0.2** (a fresh install from that
  morning): first push created the list with 2
  matched + 'Power Rangers Prime #1: series missing'; second push after
  Kavita's scan caught up **updated in place to 3 matched** — item
  order and contents confirmed on Kavita's side. Test list deleted
  after verification.

## Homelab / infra notes

1. **Homelab Kavita is docker, v0.9.0.2** (host/port in session
   memory, not here — public repo).
   It was a fresh install with NO library — I created the 'Comics'
   library via API (type 5 = Comic/ComicVine parsing, folder /comics,
   cbz-only file types, `**/@eaDir/**` excluded). Initial scan was
   still running at session end (1900+ series in /comics). Library
   creation does NOT auto-scan reliably — POST /api/library/scan was
   needed; lastScanned stays epoch until the full scan completes.
2. Kavita API key is per-session-paste (user's user-level key, admin).
3. **v1.1.11 tagged** on 0ee36f7 → docker workflow updates :latest.
   The homelab (still on 1.1.9) gets migration 13 + 14 + reading lists
   + pull list on its next manual pull. Migration 13 rewrites stored
   download-client settings JSON — rTorrent category carries over
   (dev-verified in Session 20).
4. Dev instance: test list + connections cleaned up, instance stopped,
   dev DB back to pre-test state.

## Next actions

1. **Homelab pull v1.1.11** and verify: migrations 13+14 apply clean,
   reading lists page live, rTorrent connection still works.
2. Add the homelab Kavita as a connection in homelab Panelarr (enable
   'Send Reading Lists') and push a real arc once its scan finishes.
3. Backlog: staging-folder source mount on the homelab container (real
   Import from Staging); Metron enrichment; reading-list reordering UI;
   130 unused translation keys (usage scan); Issueshelf rename.
4. Standing watch: quality 'Unknown' in Activity manual-import modal
   (unreproduced since Session 18).

## Reading list

docs/story-arcs.md (Phase C section updated to SHIPPED),
docs/feature-landscape.md (#2/#3 now shipped end-to-end).
