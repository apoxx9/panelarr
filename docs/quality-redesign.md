# Quality Model Redesign — Source-Based Ladder

Design agreed end of Session 15 (2026-06-11). **IMPLEMENTED in the Session 15
addendum** — all checklist items done and live-verified. Two deviations the spec
missed, found during verification:

1. **History/Blocklist/PendingReleases rows could not be left untouched**:
   `Quality.FindById` throws on retired ids, so loading any old row crashed
   (live: /activity/history 500'd). Migration 010 weight-maps their stored ids
   (CBR→Scan, CBZ Web→C2C, CBZ→Archive, CB7→WebRip, CBZ HD→Digital). The same
   applies to QualityDefinitions rows — the startup reconciler crashes loading
   retired ids before it can delete them, so the migration deletes them.
2. **Filename source tags now feed the import side too**: AggregateQuality
   parses the extension-stripped filename (so ".cbz" can't read as a token)
   between embedded tags and folder/client titles, and keeps the filename's
   fix-marker revision even when quality comes from the extension fallback.

Live verification (manual-import evaluation vs an existing Archive file):
untagged → Archive/accept; (Digital) → Digital/accept upgrade; (Digital) (f) →
Digital rev2; (Scan) → Scan/reject "Not an upgrade".

## Why

The current ladder is container-based (CBR < CBZ-Web < CBZ < CB7 < CBZ-HD), which
is broken two ways:

1. **Container isn't quality.** A `.cbz` of a blurry scan is worse than a `.cbr`
   of the retail digital rip. Real releases signal quality via source tags —
   `(Digital)`, `WebRip`, `c2c`, `(Scan)` — none of which the parser understands.
   CBZ-Web/CBZ-HD are unreachable (no real-world token maps to them).
2. **Remote releases don't state their container**, so everything parses Unknown.
   Two hacks paper over this: the search path silently assumes CBZ
   (Parser.cs:343 — same release shows different quality in search vs RSS), and
   quality upgrades are impossible once any file exists (Unknown remote < CBZ
   file, always).

## Decisions (all confirmed with Lorenzo)

- **Direction**: source-based ladder (not container patching, not CF-only flattening).
- **Archive placement**: above paper sources, below known-digital sources.
- **HD**: no ladder rung — handled by custom formats only (Comic Image Resolution
  condition import-side, release-title regex remote-side).
- **Defaults**: new-install profile allows Scan→Digital + Archive, cutoff Digital;
  Unknown/PDF/EPUB disallowed by default (opt-in).

## The ladder

Rule: paper sources < unknown provenance < known digital sources.

| Id | Name    | Weight | Meaning                                            |
|----|---------|--------|----------------------------------------------------|
| 0  | Unknown | 1      | Didn't parse as anything                           |
| 1  | PDF     | 10     | PDF format (kept id)                               |
| 2  | EPUB    | 20     | EPUB format (kept id)                              |
| 8  | Scan    | 30     | Tagged scan                                        |
| 9  | C2C     | 35     | Cover-to-cover scan (complete, still paper source) |
| 10 | Archive | 40     | Comic archive, no source tag (statistically a digital rip) |
| 11 | WebRip  | 45     | Ripped from a web reader (digital source, re-compressed) |
| 12 | Digital | 50     | Retail digital rip                                 |

Ids 3–7 (CBR/CBZ/CB7/CBZ-Web/CBZ-HD) are **retired** — new ids for new rungs so
stale serialized references can't silently mean something new.

Key property: untagged remote releases and untagged imported files both land on
Archive, so search/RSS/disk agree, nothing churns (Archive vs Archive is never an
upgrade), and a `(Digital)` release genuinely upgrades an untagged library.

## Parser

- Tokens (case-insensitive): `digital` → Digital; `webrip|web-rip|(web)` → WebRip;
  `c2c|cover to cover` → C2C; `scan|scanned` → Scan. Precedence when multiple
  match: Digital > WebRip > C2C > Scan (most specific wins).
- `.pdf`/`.epub` extension or explicit token → format rungs (below everything —
  the format is the limitation).
- A title that parses as a comic but has no source token → **Archive** (this
  replaces the Parser.cs:343 CBZ assumption, which is deleted). Unknown shrinks
  to "didn't parse at all".
- `(f)` / `(f2)` / `(Fixed)` → **Revision** bump (v2, v3…), wiring fix-releases
  into the existing proper/repack machinery. Keep existing `v2` digit parsing.
- `MediaFileExtensions`: `.cbz/.cbr/.cb7` → Archive; `.pdf` → PDF; `.epub` → EPUB.

## Migration (010)

1. **QualityDefinitions**: replace rows with the new set (table is seeded data,
   not user data, except custom Min/MaxSize — carry sizes over by old→new map
   below where sensible, else defaults).
2. **Quality profiles**: rebuild each profile's items to the new ladder.
   - Any of CBR/CBZ/CB7/CBZ-Web/CBZ-HD allowed → allow Scan, C2C, Archive,
     WebRip, Digital.
   - PDF/EPUB/Unknown allowed flags carry over directly.
   - Cutoff CBR/CBZ/CB7/Web/HD → Digital; PDF/EPUB cutoffs carry over.
3. **ComicFiles.Quality** (embedded JSON): re-parse `SceneName ?? OriginalFilePath
   ?? Path` with the new source parser; fallback Archive for comic-archive
   extensions, PDF/EPUB by extension. String work only — no disk I/O, no events,
   no re-downloads (Archive-vs-Archive ties).
4. **History/Blocklist rows**: leave untouched (display-only; *arr convention).
5. PendingRelease.ParsedIssueInfo rows: leave; re-parsed naturally on processing.

## Stays in custom formats (not the ladder)

- HD / image resolution (ComicImageResolutionCondition + title regex).
- Container preference (.cbz over .cbr) — extension regex on filename.
- Page-count shaping (TPB vs single issue), release groups.

## Implementation checklist (Session 16)

- `Quality.cs` (new statics, All, DefaultQualityDefinitions), `QualityParser.cs`
  (source tokens, revision (f) mapping, drop dead codec tokens),
  `MediaFileExtensions.cs`, delete Parser.cs:343 CBZ assumption,
  `QualityProfileService` default profile seed, migration 010.
- Frontend: verify no hardcoded container-quality names (quality names flow from
  the API; check NamingModal quality token examples).
- Tests to rewrite/extend: `QualityParserFixture` (new vocabulary, precedence,
  (f)→revision), `QualityModelComparerFixture`, profile + migration fixtures,
  `AggregateQualityFixture` (extension fallback now yields Archive, not CBZ),
  decision-engine fixtures that construct container qualities.
- Live verification: import untagged CBZ → Archive; interactive search shows
  (Digital) release as upgrade; grab/import round-trip; second search shows no
  further upgrade; (f) release upgrades same-source file.
