Panelarr embeds **ComicInfo.xml** and **MetronInfo.xml** into CBZ files so readers like Kavita and Komga get series, issue, and credit metadata without their own lookups.

## When tags are written

**Settings → Media Management → Write Issue Tags** controls it:

- **New files** (default) — only freshly imported downloads are tagged. Files imported from an existing library keep whatever tags they came with (embedding replaces ComicInfo.xml wholesale, so this default protects tags written by other tools).
- **All files / Sync** — Panelarr also (re)tags existing files as they're added or renamed.

## Retagging

**Retag Files** (per series or per file, from the series page) re-embeds metadata on demand, regardless of the setting above — an explicit command is always honored. It's safe to run repeatedly:

- Files whose embedded tags already match are **skipped** — no archive rewrite, no modified-time churn.
- Every retagged file gets a **history entry with the field-by-field diff** (old → new).
- Reader connections with **On Issue Retag** enabled get a targeted scan, so updated tags show up in Kavita/Komga right away.

Only CBZ files can be tagged — convert CBR files first (below).

## Converting to CBZ

**Convert to CBZ** (series page) repacks CBR/RAR archives as CBZ with verification of the result. **Settings → Media Management → Convert on Import** does the same automatically for every new download before tags are written, so your library converges on tagged CBZ over time.
