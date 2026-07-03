# Migration rehearsal — real Mylar library slice (Session 16)

A copied, Mylar-managed slice of the real library (20 series folders under
publisher directories, 269 files, 19 GB: all of Boom Studios + an Image
subset) was run through the full tagged-import flow on the dev instance.
The rehearsal both **validated the Phase 2 design and caught four real
defects** that would have derailed the homelab migration.

## Defects found and fixed (all committed with tests)

1. **Identification crashed on provider-built issues** —
   `LocalEdition.PopulateMatch` dereferenced `SeriesLinks`, which issues
   built from provider search results never populate. Every crashed file
   was silently skipped and never inserted as unmapped — invisible to
   Library Import. (`cdcecc6`)
2. **Scans burned 2–3 rate-limited provider calls per unknown file** —
   `RescanFoldersCommand` defaulted `AddNewSeries=true`, unlocking a
   remote-search leg whose results a scan can never import anyway. The
   269-file scan took 45+ minutes and would have taken days at library
   scale; after the fix (scans identify against the library only) it took
   **~15 seconds**. (`cdcecc6`)
3. **Fuzzy matches auto-imported into the wrong series** — the scan
   imported `Power Rangers #1 (2020)` into *Power Rangers Prime (2024)*
   on name distance alone. Root scans now auto-import only exact
   tag-id matches (or files inside a series' own folder); fuzzy results
   land as unmapped for review. A related UNIQUE-constraint crash
   (candidate context files from other folders being re-inserted) was
   fixed by scoping unmatched inserts to the scanned folders. (`d8b04b7`)
4. **Tag-fallback proposals used the issue id as the series id** — both
   providers' issue payloads name the parent series but the mapping layer
   dropped it and the proxy echoed the issue id back. Now carried through
   (`ProviderIssue.ForeignSeriesId/SeriesName`), with a proposal-side
   guard and folder-year display precedence. (`5f4d7a9`)

## Final run results (fixed build, clean DB)

- **Scan**: 273 files → 272 unmapped + **1 auto-import** (a TWD copy
  exact-id-matched the library's only file-less issue — correct), ~15 s,
  zero errors, zero provider calls.
- **Proposals**: **20/20 folders proposed, all exact** — 19 via cvinfo
  (zero provider calls), 1 via file tags (one issue-detail call:
  Saban's Go Go Power Rangers → cv:102945). 0.2 s. TWD correctly flagged
  already-in-library and excluded.
- **Import**: 19 series added in 24 s (sequential, unmonitored), refresh
  wave fetched issue lists and mapped files in under a minute.
- **Mapping**: 264/272 files mapped (97%), **zero wrong-series
  mappings** (verified in SQL: the Power Rangers folders each map to
  their own volumes; nothing landed in Prime).
- **Files byte-untouched**: pre/post SHA-256 verified on samples across
  three series.

### The 8 expected stragglers (v1-correct behavior, future features)

| files | why unmapped |
|---|---|
| 4× MMPR The Return #01–04 **.pdf** | duplicate lower-quality copies of already-mapped CBZs — correctly held back by the quality ladder (PDF is the lowest rung) |
| 3× MMPR **Annual** #01 (2016/17/18) | the classic annuals-matching problem (see feature-landscape: Mylar's annuals handling is chronically fragile too); manual-map for now |
| 1× Radiant Black **#025.5** (.cbr) | investigated: not a Panelarr bug — ComicVine's volume has no 25.5 (sequence runs 25, 26, 26.5…); the tagger's numbering disagrees with CV. Manual-map it to the issue it actually is. Fractional numbers that DO exist in the volume (26.5–30.5) matched fine |

Plus 31 TWD duplicates staying unmapped because the library's copies are
equal quality — correct.

## Measured scale datapoints (for scale-readiness.md)

- Local-only identification: ~18 files/s on 19 GB of archives (tag reads
  dominate). A 50k-file migration scan ≈ 45–50 min, zero provider calls.
- Proposal generation: 20 folders in 0.2 s; provider calls only for
  non-cvinfo folders (1 per such folder).
- Import: ~1 s per series + refresh (provider-throttled) afterwards.

## Notes for the real homelab migration

- Publisher-level folder nesting (`Comics/Publisher/Series/`) works
  end to end.
- One `.cbr` had a bad RAR signature (mislabeled archive?); it still
  mapped, but archive inspection logged a warning — expect a few of these
  at full scale.
- Monitored=false on import kept the indexer search surface silent; flip
  monitoring on deliberately after review.
- Remaining unmapped files after a migration are a *worklist*, not
  failures: duplicates, annuals, and oddballs surface there by design.
