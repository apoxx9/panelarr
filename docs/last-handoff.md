# Last Handoff — 2026-07-07 (Session 21)

Session 21 shipped **Reading Lists Phase C** (push to Kavita/Komga),
then **merged feature/story-arcs to main** (fast-forward, 0ee36f7) and
tagged **v1.1.10** — the release that finally carries the whole batch:
fossil cleanup + **migration 13** (Music*/Tv* download-client settings
rename) + **migration 14** + Reading Lists A+B+C. Suite: **2473 / 0**.

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
3. **v1.1.10 tagged** on 0ee36f7 → docker workflow updates :latest.
   The homelab (still on 1.1.9) gets migration 13 + 14 + reading lists
   + pull list on its next manual pull. Migration 13 rewrites stored
   download-client settings JSON — rTorrent category carries over
   (dev-verified in Session 20).
4. Dev instance: test list + connections cleaned up, instance stopped,
   dev DB back to pre-test state.

## Next actions

1. **Homelab pull v1.1.10** and verify: migrations 13+14 apply clean,
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
