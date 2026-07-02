# Publisher UI — design draft (Session 15 overnight, for morning review)

Status: **Tier 1 + Tier 2 Option A SHIPPED** (Session 16). Publisher filter
on the series index (plus fixing the publisher column and sort, which were
still reading Readarr's dead `disambiguation` field), and a
"Group by Publisher" toolbar toggle rendering section headers in all three
index views (posters, table, overview). Grouping is publisher-alphabetical
with the user's sort preserved within groups; the jump bar hides while
grouped; the toggle persists. Tier 2 Option B (dedicated /publishers pages)
and Tier 3 remain unbuilt — open choices below marked ⚖.

## What exists today

The backend is essentially done already:

- `Publisher` entity (src/NzbDrone.Core/Issues/Model/Publisher.cs):
  ForeignPublisherId, Name, CleanName, Description, Images (logo art via
  MediaCover).
- `SeriesMetadata.PublisherId` + lazy-loaded `Publisher` — every series knows
  its publisher; populated from the metadata provider on add/refresh.
- Full CRUD API at `/api/v1/publisher` (PublisherController) — list, get,
  create, update, delete. Nothing in the UI calls it today.
- `SeriesResource.PublisherName` is already on the wire for every series; the
  series index table has a Publisher column and sort
  (frontend/src/Series/Index/Table/SeriesIndexRow.js:164,
  SeriesIndexSortMenu.js:32).

So "Publisher UI" is a frontend feature with at most small API additions. No
migrations needed.

## Why (the user story)

A migrated Mylar library has hundreds of series across a handful of
publishers. Publisher is the natural top-level shelf for comics the way
"author" is for books: "show me my Image stuff", "what DC series am I
monitoring", "everything Boom! that's unmonitored". Mylar surfaces publisher
as a primary column; we have the data but no way to browse by it.

## Proposed scope, smallest-first

### Tier 1 — filter where people already are (ship first)

Add Publisher to the series index **filter** menu (it's already a sort).
Custom filters in *arr UIs support equals/contains on string fields; wiring
`publisherName` into the filter builder gives "Publisher is Image" views and
saved custom filters for free. Smallest possible change, immediately useful
at migration scale.

### Tier 2 — publisher grouping on the series index

⚖ Option A (recommended): a "Group by publisher" toggle on the existing
index (poster and table views), rendering section headers per publisher —
similar to how Sonarr groups the calendar by date. One page, no new routes,
reuses all existing index machinery (selection, bulk edit still work).

⚖ Option B: a separate `/publishers` index page — grid of publisher cards
(logo from `Publisher.Images`, series count, monitored count) → click
through to a publisher detail page listing its series. Prettier and matches
"Publisher UI" most literally, but it's a new top-level page, new route, new
Redux store section, and duplicates series-list rendering. More to maintain.

A can ship alone; B can layer on later using the same data. Starting with B
and skipping A would leave the main index unimproved.

### Tier 3 — publisher detail niceties (later, optional)

- Publisher page header: logo, description (both already in the model).
- Per-publisher rollups: series / issues / files / disk size, monitored
  split. Needs one cheap aggregate endpoint or client-side reduce over the
  already-loaded series list (fine at our scale since the index loads all
  series anyway — see scale-readiness doc before promising more).
- Bulk actions scoped to the publisher (monitor/unmonitor all) — already
  possible via select-all within a filtered view, so only worth it if Tier 2
  Option B exists.

## API gaps (small)

- `/api/v1/publisher` returns no series counts. For Tier 2 B cards either:
  count client-side from the series list already in Redux (zero backend
  work, recommended), or add `seriesCount`/`monitoredCount` to
  PublisherResource via one grouped query.
- Publisher images: **verified empty tonight** — the live dev DB has
  publishers (DC Comics, Boom! Studios, Image…) with names populated but
  `images: []` for all of them. The CV volume payload only carries publisher
  id+name; logo art would need the separate `GetPublisher` detail call
  (already implemented in ComicVineProvider.GetPublisher but apparently not
  invoked during refresh). So Tier 2 B cards should design for the
  initials-tile fallback first; logo fetch is its own small backend
  increment.

## Open questions for the morning ⚖

1. Tier 2: grouping toggle (A), publisher pages (B), or A now + B later?
2. Should publisher become a first-class sidebar entry ("Publishers" under
   Library), or stay a view of the series index?
3. Mylar parity check: does the user want publisher-level *monitoring*
   semantics (auto-add new series from a publisher)? That's an import-list
   feature, much bigger, and overlaps with the feature-landscape doc —
   explicitly out of scope here.
4. Terminology: "Publisher" vs ComicVine's occasional "imprint" distinction
   (DC/Vertigo). The model has no imprint concept; treat imprints as
   publishers as CV reports them (recommended), or model the hierarchy
   (not recommended for v1)?

## Effort gut-call

- Tier 1: small — filter-builder wiring + en.json strings.
- Tier 2 A: medium-small — index grouping render path.
- Tier 2 B: medium — new page, route, store slice, cards, detail view.
- Tier 3: small increments on top of B.
