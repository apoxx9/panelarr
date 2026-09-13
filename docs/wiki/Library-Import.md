**Library → Library Import** brings existing files into Panelarr. There are two workflows on the page.

## Import an existing collection (in place)

Point the scanner at a folder that already contains your organized comics. Panelarr scans, proposes series matches (using embedded ComicInfo tags and `cvinfo` files where present, falling back to folder-name matching), and lets you review every proposal before importing. Files stay where they are; the folders become tracked series.

## Import from Staging

For new files that should be *filed into* your library (e.g. a seedbox download folder), use **Import from Staging**:

1. Set the **Staging Folder** under Settings → Media Management → Importing (the picker on the import page defaults to it).
2. Scan — Panelarr proposes what each file is, including matches against **series you already track** (filing new issues into an existing series is the main use case).
3. Pick the target root folder, quality profile, and whether to keep source files, then import.

Every file gets a line in the **per-file report**: imported, rejected (with the reason), or failed. Files left in staging always tell you why — for example, a duplicate at equal quality is rejected rather than silently replacing your library copy (only genuine quality upgrades replace files).
