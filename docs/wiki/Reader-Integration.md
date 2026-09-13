Panelarr integrates with **Kavita** and **Komga** through connections (Settings → Connect).

## Library scans

With **Update Library** enabled on the connection, Panelarr triggers a targeted library scan in your reader whenever it imports, upgrades, retags, or deletes files — so new issues appear in the reader without waiting for its scheduled scan.

- **Kavita** — needs host, port, and your Kavita API key (Kavita → User Settings → API key). The scan targets the affected folder.
- **Komga** — needs the base URL and user credentials.

## Reading list push

With **Send Reading Lists** enabled on the connection (off by default), the **Push to Readers** button on a [[Reading Lists|reading list]] sends that list to the reader:

- **Kavita** — imported through Kavita's own CBL matcher (v0.9+ multi-step flow; v0.8 supported via fallback). Re-pushing updates the same-name list in place. Kavita matches best when your files carry embedded ComicInfo metadata — which Panelarr writes.
- **Komga** — entries are matched by series name + number against Komga's library; the readlist is created, or updated if one with the same name exists. Ambiguous matches are skipped and reported rather than guessed.

Push results are shown per connection: created/updated, matched count, and any entries the reader couldn't match.

Because it embeds ComicInfo.xml and MetronInfo.xml into your CBZ files and controls the file naming, Panelarr-managed libraries generally match cleanly on the reader side by construction.
