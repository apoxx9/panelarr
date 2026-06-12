# Comic-manager feature landscape (Session 15 overnight, research only)

What the neighboring tools do, and the most promising gaps for Panelarr.
Researched 2026-06-12 night from the projects' own docs/wikis (sources at
the end). Nothing here is a commitment — it's the menu for deciding what
comes after the migration tooling.

## The field at a glance

| Tool | What it is | Defining strength |
|---|---|---|
| Mylar3 | the incumbent comic *arr | weekly pull list, story arcs, one-offs |
| Kapowarr | newer Mylar alternative | library-import wizard, DDL-first, CBR→CBZ pipeline |
| Komga | reader/server | CBL reading lists, duplicate detection, OPDS |
| Kavita | reader/server | series relationships, smart filters, scrobbling |
| Sonarr/Radarr | the family Panelarr inherits from | custom formats, import lists, collections |

### Mylar3 (what our migrating user is leaving)

- **Weekly pull list** — upcoming releases up to 4 weeks ahead; watched
  series auto-marked Wanted. Its killer feature; nothing else has it.
- **One-off downloads** — grab a single issue without watching the series.
- **Story arcs** — CV arc search, cross-series arc tracking, CBL
  (ComicRack reading list) import, "search missing in arc".
- **Annuals merged into parent series** (toggleable; chronically fragile —
  repeated fixes upstream).
- Booktype support (Print/TPB/GN/One-Shot), GetComics DDL scraping,
  bundled ComicTagger metatagging, series.json sidecars.

### Kapowarr

- Volume-centric model mirroring ComicVine; special-version classification
  (TPB/One-Shot/Hardcover) with separate naming — Panelarr already has the
  per-type naming half.
- **Library import wizard** — scan, propose CV matches, review, import or
  import+rename. Was best-in-class until tonight; notably it does NOT
  exploit embedded tags/cvinfo the way our Phase 2 now does.
- DDL-first (GetComics with Pixeldrain/Mega/MediaFire handling),
  CBR→CBZ conversion with format-preference fallback, mass editor.

### Komga / Kavita (readers — integrate, don't compete)

- Both: CBL reading-list import, collections, metadata editing, OPDS,
  per-user libraries. Komga adds duplicate file/page detection and
  Kobo/KOReader sync; Kavita adds typed series relationships
  (prequel/sequel/spin-off), smart filters, AniList/MAL scrobbling.

### Metadata standards

- ComicInfo.xml is now governed by the Anansi Project (v2.0 stable, v2.1
  draft); MetronInfo.xml is the typed successor (proper arrays, sourced
  ids, GTIN) designed to coexist in the same archive. Panelarr already
  writes both — ahead of the field here.
- A Mylar library is tagged in ComicTagger conventions; **round-tripping**
  (read, trust, preserve) matters more than writing fresh tags. Phase 1's
  embed-gating fix was exactly this lesson.

## Top gaps for Panelarr, ranked for a Mylar migrator

1. **Weekly pull list** (M) — the #1 thing a Mylar user will miss. Comics
   are a weekly medium; the pull list is the unit of consumption. We
   already consume Metron release dates; build it as a comic-native view
   on the inherited *arr calendar (ship plain calendar first — S).
2. **Story arcs / events** (M–L) — first-class arc entity (Metron/CV both
   provide arc records): membership across series, "search missing in
   arc". Radarr's Collections is the family pattern to copy. Without it,
   "do I have all of Secret Wars?" is unanswerable.
3. **CBL import/export** (S–M, piggybacks on #2) — .cbl is the lingua
   franca: Mylar builds arcs from it, Komga/Kavita import it as reading
   lists. Import to create arcs; export arcs; optionally push lists to
   Komga/Kavita through their APIs (turns our scan-trigger integration
   into real curation).
4. **Want list / one-off grab** (M) — fetch one issue/TPB without adding
   the series. Known Mylar-vs-*arr friction; needs an unparented-item
   concept in the domain model.
5. **Custom formats, comic-aware** (M) — port the Radarr/Sonarr v4
   scoring system with comic conditions (scanner group, digital vs c2c,
   webrip). This was already chosen this session as the path for
   HD-vs-SD preference within the new quality ladder — this ranking
   reinforces that decision.
6. **Import lists from Metron/CV** (M) — follow a publisher, creator, or
   character with auto-add. No comic tool has it; the *arr plumbing is
   inherited and idle. Pairs naturally with the Publisher UI draft.
7. **TPB ↔ issue coverage duality** (L) — owning the trade satisfies the
   floppies. Nobody has solved it; Metron's collection-contents data makes
   it newly feasible. Genuine differentiator, big modeling lift.
8. **DDL sources** (L + policy decision) — in practice weekly comics live
   on DDL (GetComics); indexer coverage is thin. It's also scraping-fragile
   and sits awkwardly in the indexer/download-client abstraction. Needs an
   explicit project-policy call before any technical work.
9. **ComicTagger round-trip fidelity** (S–M) — largely done (Phase 1 +
   embed gating); remaining: preserve unmanaged fields when we do rewrite
   tags, and match ComicTagger field semantics exactly. Audit-sized task.
10. **Publisher browsing** (S) — see docs/publisher-ui.md; backend exists,
    cheap UI win.

Evaluated and deprioritized: annuals-merging à la Mylar (their
implementation is chronically buggy; our separate-type model may simply be
better — revisit only on user demand), duplicate-page detection and OPDS
serving (reader territory; Komga/Kavita do it better — keep integrating).

## Sources

- github.com/mylar3/mylar3 (+wiki, FAQ, usage docs)
- github.com/Casvt/Kapowarr + casvt.github.io/Kapowarr (library import,
  media management docs)
- komga.org, github.com/gotson/komga
- kavitareader.com, github.com/Kareadita/Kavita, wiki (relationships)
- anansi-project.github.io (ComicInfo), metron-project.github.io
  (MetronInfo), github.com/comictagger/comictagger
- wiki.servarr.com (Sonarr v4 custom formats, import lists), Radarr
  collections docs, trash-guides.info
