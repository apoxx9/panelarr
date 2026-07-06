# Reading Lists (story arcs) — design SETTLED 2026-07-06 (Session 20), Phase A built

Status: **Design settled with the user** after the research below. The
governing requirement: arcs curated in Panelarr must be consumable by
Kavita/Komga — which makes **the CBL file the interoperability contract**,
not an afterthought. Development happens **isolated on a feature branch**
(main auto-builds :main and the homelab pulls released tags; arcs merge
only after Phase A+B are verified end-to-end).

## 0. Settled design summary

0. **The feature is named "Reading Lists"** (settled after Phase A was
   first built as "Arcs"): the CBL format is literally a ReadingList and
   Kavita — the consumption target — uses the same name, giving a 1:1
   mapping across the toolchain. "Arc" is a TYPE of reading list
   (arc / event / readingOrder / custom). Entity ReadingList, slots
   ReadingListItem, API /api/v1/readinglist. Import never touches the
   library; Phase B ships an EXPLICIT "add missing series" affordance
   (root folder + quality profile picker) on the list detail page.

1. **The model IS the CBL shape, enriched.** An arc is an ordered list of
   slots; each slot carries `Position`, nullable `ForeignIssueId` (cv id),
   denormalized display fields (SeriesName, IssueNumber, Volume/start-year,
   Year), and a nullable resolved link to a library issue. A lossless CBL
   round-trip (including the `<Database Name="cv">` id extension) is an
   acceptance criterion, not a nice-to-have.
2. **Ids first, names as fallback.** The library is cv-id native, community
   CBLs carry cv ids, Kavita matches ids at its highest-confidence tier.
   Name matching exists only for old id-less CBLs; unresolved slots persist
   visibly with a reason (never silently dropped) — Mylar's name-based rows
   are where its arc bugs live.
3. **We control both ends of the Kavita handshake.** Panelarr names the
   files and writes the ComicInfo tags Kavita reads, so exported CBLs use
   series names that match by construction.
4. **Slots survive library churn.** Deleting a series/issue UNRESOLVES its
   slots (link nulled, display fields + foreign id kept) — the curation is
   the asset; the library link is derived. (Deliberately different from
   series-relations, which cascade-delete.)
5. **No stored per-slot status.** Have/missing computed live from the
   slot→issue join. (Mylar persists Status and it drifts.)
6. **`Type` field from day one** (arc / event / readingOrder / custom) —
   the CBL hubs are full of creator runs and master reading orders, not
   just strict arcs. (Superseded by point 0: the user-facing name is
   "Reading Lists"; arc is the default type.)
7. **Settled ⚖s**: TPB/collected-edition filter ON for CV imports (with a
   "N skipped" note); **track-only in v1** — no monitored/auto-add
   semantics, coverage + explicit "search missing" only (not repeating
   Radarr's loudest complaint); **CV-only provider in v1** (it's the id
   spine), Metron enrichment is the first follow-up; images skipped in v1
   (initials tile); reordering UI later (positions are ours from day one).
8. **CBL export is core v1** (moved up from phase 3): serialize slots to
   CBL v1 XML with cv ids. Kavita/Komga API push is a fast follow riding
   the existing reader connections.

The remainder of this doc is the research record and the original
reasoning that produced the summary above.

---

## 1. What the data sources actually give us

### ComicVine (our id spine — every library issue is `cv:*`)

- `story_arc` resource: has `publisher`, `deck/description`, `image`, and an
  `issues` array — but each entry is **only `{id, name}`**. No issue
  numbers, no dates, **no ordering of any kind** (confirmed via schema and
  CV dev-forum consensus). Ordering must be derived by fetching the arc's
  issues (`/issues/?filter=story_arc:{id}` returns cover/store dates in
  paged lists) and sorting by cover date.
- Arc search works server-side (`/search?resources=story_arc` and
  `/story_arcs?filter=name:`).
- Issue detail has `story_arc_credits` (reverse membership) but it is
  crowd-edited, uneven, and has a known field_list bug — treat as bonus
  data, never as the source of truth.
- Community gotcha (via Mylar users): CV arcs freely mix **TPBs and
  single issues** in the same arc — an unfiltered import pollutes the arc.

### Metron

- `GET /arc/{id}/issue_list/` is a dedicated, **server-ordered**
  (cover_date, series, number) paged list with rich entries (series
  name/volume/year, number, dates, cover image).
- Arc records carry **`cv_id`** — free cross-provider mapping onto our id
  spine. Arc list filters by `name` and by `cv_id`.
- No publisher/year on the arc itself (derive from issues). Rate limit is
  tight: **20/min** (mokkari + server settings agree).
- Metron separately has a `reading_list` resource (curated orders) — a
  different thing from arcs; interesting for a later phase, not v1.

### CBL files (ComicRack reading lists — the community lingua franca)

- XML: `<ReadingList><Name/><Books><Book Series Number Volume Year>` where
  `Volume` is conventionally the series **start year**. Community lists
  (DieselTech/CBL-ReadingLists — the de-facto hub, CV-verified) add a
  `<Database Name="cv" Series="<cv volume id>" Issue="<cv issue id>"/>`
  child per book. **File order IS the curated reading order** — that's the
  entire value of a CBL.
- Consumers differ: Mylar3's new importer *requires* the CV ids; Kavita
  uses a tiered matcher (ids → exact name → pattern → stripped-article →
  user remap rules, with enumerated failure reasons and an import report);
  Komga matches `Series (Volume)` + number only and lets the user fix
  misses manually.

## 2. Prior-art lessons

### Mylar3 (what our user is used to)

- One denormalized table row per arc-issue: arc id, series name, issue
  number, `ReadingOrder` int, per-row Status (Wanted/Snatched/Downloaded…),
  CV ids where known. Arc detail = ordered checklist with have/missing;
  "search missing in arc" marks rows Wanted and queues searches.
- Does **not** auto-add unwatched series: issues from outside the
  watchlist are grabbed as "one-offs" into a per-arc folder (grab-bag).
  The new CBL import screen is the path that can add whole volumes.
- Chronic bug surface: annual-integration breaks arc linking, apostrophes
  in arc names, TPB pollution from CV, arc-folder year confusion. The
  denormalized name-based rows are the root of much of it.

### Radarr Collections (the family pattern)

- Entity + **derived membership** (movies point at collection id; TMDB is
  source of truth); collection carries add-defaults; scheduled 24h refresh.
- Two loud complaints to deliberately avoid:
  1. Collections are **system-managed** — no user create/delete. Users
     hate not being able to remove one.
  2. **Monitored ⇒ auto-add**, with no "just track coverage" mode.
- Sonarr has nothing comparable (no clean provider concept for TV).

## 3. Proposed design (recommendation, not settled)

Arcs are **user-curated first-class entities** — the opposite lifecycle
choice from Radarr, because arcs are curation (a reading list with
provider help), not provider facts about library items.

### Model

- `Arcs`: Id, Name, ForeignArcId (nullable `cv:*` — null for manual/CBL-only
  arcs), Publisher, Description, Image, Added. (⚖ Monitored — see Q2.)
- `ArcIssues` (one row per slot, like Mylar but normalized where possible):
  ArcId, **Position** (int — the reading order), IssueId (nullable FK to a
  library issue, the resolved link), ForeignIssueId (nullable `cv:*`),
  plus denormalized display fields for unresolved slots: SeriesName,
  IssueNumber, Year.
- **Positions live on our rows, not the provider.** CV can't order at all;
  Metron orders by cover date; CBL files carry true curated order. Whatever
  the source, we snapshot an order the user can keep (and later reorder).
- Resolution: a slot with a ForeignIssueId joins to the library by foreign
  id (exact, cheap — our whole library is `cv:*`). Name-based slots (old
  CBLs) get tiered matching (Kavita-style: id → exact clean name+number →
  unresolved). Unresolved slots stay visible as display-only rows with a
  reason — same philosophy as the staging-import report.

### Sources (three ways an arc gets created)

1. **Provider arc search** (CV primary since it's the id spine; ⚖ Metron
   enrichment — see Q4): search by name → pick → fetch membership → order
   by cover date → create arc + slots.
2. **CBL import**: parse file, use `<Database Name="cv">` ids when present
   (DieselTech lists all have them), fall back to name matching; file
   order = positions; import report lists unresolved slots and why.
3. **Manual**: create empty arc, add issues from library (nice-to-have,
   can trail).

### What arcs do NOT do (v1)

- **No auto-add of series** (Radarr complaint #2, Mylar agrees). The arc
  detail page shows not-in-library slots and can offer an explicit
  "add series" affordance per slot later; nothing automatic.
- **No one-off/grab-bag downloads** (that's feature-landscape #4, a
  separate domain problem).
- **No CBL export / Komga-Kavita push** — phase 2 of #3, cheap once the
  model exists.

### UI sketch

Library → Arcs: card/list index with coverage (have X of Y, resolved vs
total). Arc detail: ordered slot table (position, series, issue, year,
status: have / missing / not-in-library / unresolved), "Search missing"
(IssueSearch for resolved+monitored slots — same mechanics as the pull
list), CBL import on the index page, refresh-from-provider and delete on
the detail page.

## 4. ⚖ Open questions for discussion

1. **TPB pollution filter.** CV arcs mix TPBs with singles. Options: filter
   by our existing skip-collected-editions logic at import time (flag but
   don't create slots), or import everything and let the user delete slots.
   Leaning: filter with a "N collected editions skipped" note.
2. **Does an arc have monitored semantics at all in v1?** Track-only keeps
   v1 honest (coverage + manual search button). "Monitored arc" could mean:
   auto-include arc issues in missing-search tasks. Leaning: ship
   track-only, add monitoring later if it earns its keep — explicitly NOT
   Radarr's monitored⇒auto-add.
3. **Slot statuses vs live computation.** Mylar persists per-row Status and
   it drifts. We can compute have/missing live from the IssueId join
   (cheap at our scale) and persist nothing. Leaning: compute live.
4. **Metron's role.** Its ordered issue_list + cv_id mapping is strictly
   better arc data, but 20/min hurts and our composite provider already
   has per-provider plumbing. Options: CV-only v1; or Metron-preferred
   with CV fallback when the arc has a metron match. Leaning: CV-only v1,
   Metron enrichment as a fast follow (the `cv_id` filter makes it easy).
5. **Reordering UI.** Positions are in the model from day one; is
   drag-to-reorder in scope for v1 or later? Leaning: later (positions
   render in order; reorder can wait).
6. **Arc images**: CV arc image via MediaCover caching like series posters,
   or skip images in v1? Leaning: skip in v1, initials tile like publishers.

## 5. Phasing (settled)

Built on a **feature branch**, merged only after A+B verify end-to-end.

- **Phase A (backend)**: migration (Arcs incl. Type + ArcIssues slots), CV
  arc search/fetch in the provider layer, ArcService (create-from-provider
  with TPB filter, CBL parse + tiered match, resolve/unresolve on library
  events, delete), **CBL export**, API (search, CRUD, import report,
  export), tests incl. a lossless CBL round-trip test.
- **Phase B (frontend)**: Arcs index (coverage), arc detail (ordered slots,
  live have/missing/not-in-library/unresolved, search missing), CBL
  upload + download.
- **Phase C (follow-ups)**: Kavita/Komga API push, Metron enrichment
  (cv_id mapping), reordering UI, per-slot add-series affordance.

## Sources

CV API docs + dev forum (ordering thread, story_arc_credits bugs); Simyan
schema (strict story_arc fields); Metron server source (arc viewset,
issue_list ordering, throttle rates) + mokkari; mylar3 source
(webserve.py storyarcs table, addStoryArc, importReadlist, cblimport,
ReadGetWanted) + issue tracker (#346, #749, #1057, #1101, #1148);
DieselTech/CBL-ReadingLists (real Knightfall.cbl inspected); Kavita
develop matcher (CblSeriesMatcher/CblMatchTier/CblImportReason) + wiki;
Komga readlists docs; Radarr source (MovieCollection, RefreshCollection-
Service, CollectionController) + issues #6861/#7663/#9279/#7571.
