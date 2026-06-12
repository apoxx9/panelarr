# Terminology & UX wording audit (Session 15 overnight, research only)

Status: research deliverable — no changes made. Findings verified against
en.json and the frontend on 2026-06-12 night.

## Headline

**The user-facing layer is clean.** A scripted sweep of every value in
`src/NzbDrone.Core/Localization/Core/en.json` found **zero** strings that
show "book", "author", "album", "track" or "artist" to the user, and the
frontend has no hardcoded wrong-domain JSX text, no Readarr/Lidarr/
Goodreads/MusicBrainz/calibre references, and comic-appropriate routes
(/series, /issues, /library, /unmapped). The remaining debt is below the
surface plus a short list of genuine wording judgment calls.

## 1. Internal localization-key debt (invisible to users, worth a cleanup pass)

**130 keys** still carry Book/Author/Album/Track/Artist in the *key name*
while their values are already comic-domain. Many exist as old/new duplicate
pairs with identical values, e.g.:

| old key | new key | shared value |
|---|---|---|
| `AddNewAuthor` | `AddNewSeries` | "Add New Series" |
| `AddNewBook` | `AddNewIssue` | "Add New Issue" |
| `EditAuthor` | `EditSeries` | "Edit Series" |
| `BookMonitoring` | `IssueMonitoring` | "Issue Monitoring" |
| `StandardBookFormat` | `StandardIssueFormat` | "Standard Issue Format" |

Risk: a future contributor edits one of the pair and the UI becomes
inconsistent depending on which key a component references. Suggested
direction: one mechanical pass — point all call sites at the comic-named
key, delete the legacy keys, guard with a test that fails on
`(Book|Author|Album|Track|Artist)` in key names. Medium effort, zero user
impact, low risk. Not urgent.

## 2. Real user-visible wording items

Ordered by how often a user meets the screen.

1. **Library Import page vs. its own button** —
   [UnmappedFilesTable.js:267](frontend/src/UnmappedFiles/UnmappedFilesTable.js#L267)
   titles the page with `translate('UnmappedFiles')` ("Unmapped Files")
   while the sidebar/spec call this surface "Library Import" and the new
   button says "Scan for New Series". Three names for one page. Suggested:
   title the page "Library Import", keep "Unmapped Files" as the table
   section label for the leftovers, keep the button as is. Low effort.
2. **`Issueshelf` → "Pull List"** — the key is a Goodreads-shelf leftover
   and "Pull List" has a specific meaning in comics (your weekly list of
   new releases at the shop) that this feature is *not*. Confusing for
   exactly our target user. Decide what the feature actually is before
   renaming the label; flag for morning discussion. (Also relevant: a real
   weekly pull list is a candidate feature in the feature-landscape doc.)
3. **`SkipCollectedEditions` = "Skip Collected Editions (TPBs, Omnibuses)"**
   — wording is fine for comic people; "TPB" is standard. Leave, or
   simplify to "Skip Collected Editions". Cosmetic.
4. **`MetadataProviderSource` = "Metadata Provider Source"** — awkward
   double "source"; "Metadata Provider" reads better. Cosmetic.
5. **"Metadata Profile"** — correct *arr-ism but opaque to newcomers
   (it governs *which issue types* are wanted, not where metadata comes
   from). A help-text clarification is enough; renaming the concept isn't
   worth the churn.

## 3. Judgment calls — parked, not recommendations

- **EPUB in the quality ladder.** The sweep flagged EPUB as a text-format
  oddity for comics. Context the sweep lacked: the ladder
  (PDF < EPUB < Scan < C2C < Archive < WebRip < Digital) was deliberately
  redesigned and approved THIS session, with EPUB kept as a low rung for
  ebook-sourced comics/manga that do circulate as EPUB. No action;
  recorded here so the morning reader knows it was considered and is
  intentional, not leftover.
- **"Variant" naming.** `Edition` was renamed "Variant" UI-side; in comics
  "variant" usually means variant *cover*. If variant covers ever become a
  feature this collides. Park until then.
- **Series vs Volume.** ComicVine calls a series a "volume"; the UI says
  "Series" consistently. Consistency beats provider fidelity — no change,
  but worth one help-text mention on the add-series search (which shows CV
  volumes).

## 4. Verified clean (no action)

- Notification triggers (`OnSeriesAdded`, `OnIssueDelete`,
  `OnComicFileDelete`), health-check messages (reference ComicVine/Metron),
  status values, quality names other than the EPUB note above.
- No "audio"/music remnants anywhere user-visible.

## Suggested order if/when acted on

1. Page-title fix (#2.1) — 15 minutes, ships with any next UI change.
2. Cosmetics #2.3/#2.4 — batch into any en.json-touching commit.
3. Key-debt cleanup pass (#1) — its own mechanical PR with the guard test.
4. #2.2 and section 3 — need product decisions first (morning items).
