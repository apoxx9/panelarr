Reading lists are ordered, cross-series lists of issues — story arcs, events, and community reading orders. They live under **Library → Reading Lists**.

A reading list is *track-only*: importing one never touches your library. Each slot shows a live status — **have**, **missing** (series tracked but no file), or **not in library** — and the list header shows overall coverage.

## Creating a list

**From ComicVine** — search a story arc by name in the panel at the top of the index page and add it. Panelarr pulls the arc's issues, orders them by cover date, and filters out collected editions (TPB/HC/omnibus entries that ComicVine mixes into arcs — the import report tells you how many were skipped).

**From a CBL file** — upload any ComicRack `.cbl` reading list, e.g. from [DieselTech/CBL-ReadingLists](https://github.com/DieselTech/CBL-ReadingLists). File order is preserved as the reading order. Entries resolve against your library in tiers: exact ComicVine id first, then exact series name + issue number (disambiguated by year when several series share a name). Anything that can't be resolved stays visible with a reason — nothing is silently dropped.

## Working with a list

- **Search missing** — per issue, or "Search N missing" for the whole list.
- **Find on ComicVine** — entries that came from a CBL with only a name (no usable id) can be resolved against ComicVine per list: unambiguous matches link automatically, the rest get an inline candidate picker.
- **Add missing series** — slots whose series isn't in your library can add it explicitly (root folder + quality profile picker). This is the only path from a list into your library, and it's always your click. Series added this way default to **unmonitored**, so using a list as a collecting checklist doesn't start download hunts.
- **Relink a slot** (wrench icon) — community CBLs sometimes carry wrong ComicVine ids (duplicate volumes, TPB records). If a slot matched the wrong issue — or didn't match something you own — pick the correct series and issue; your fix is stored durably and wins over the file's claim.
- **Export CBL** — downloads the list as a `.cbl`. Resolved slots export with your library's identity (names and ComicVine ids), so a list imported from a broken community file exports as a *corrected* file. **Export all** downloads every list at once as a zip.

Because unresolved slots stay visible, a list of an entire line (say, every Epic Collection volume from a publisher's wiki page) doubles as a **collecting checklist**: owned volumes show as have, unowned ones as missing slots you can resolve and add when you buy them.

## Pushing to Kavita / Komga

With a [[Reader Integration|reader connection]] that has **Send Reading Lists** enabled, the **Push to Readers** button sends the list (as a CBL) to each opted-in reader. Only **resolved** slots are pushed — checklist entries you don't own yet stay out of your reader:

- **Kavita** — uses Kavita's CBL import (v0.9+ flow, with fallback for v0.8). A same-name list in Kavita is updated in place.
- **Komga** — matches entries against Komga's library and creates or updates the readlist by name. Only unambiguous matches are accepted; the rest are reported.

The result panel shows, per connection, whether the list was created or updated, how many issues matched, and which entries the reader could not match (with reasons).
