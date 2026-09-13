Panelarr can grab releases from direct-download sources alongside torrents and usenet. The built-in client targets **GetComics**.

## Setup

1. **Settings → Indexers → Add → GetComics** — the site is searched like any indexer, and its feed powers RSS automation.
2. **Settings → Download Clients → Add → GetComics Direct Download** — set an explicit **download folder** (required; the wizard warns when it overlaps another client's folder).

Grabs download straight into the folder and import like any completed download.

## Mirrors

A GetComics post usually offers several mirrors. Panelarr resolves them per grab and picks by reliability — Pixeldrain first, then the site's own main-server link, then Mega — falling back automatically when a mirror fails or throttles. Archives that arrive mislabeled (a RAR named `.cbz`) are detected and repacked automatically before import.

Large downloads are protected by an idle-read timeout rather than a fixed cap, so a slow multi-gigabyte mirror completes as long as data keeps flowing.

## Torrents first, direct downloads as fallback

**Settings → Profiles → Delay Profiles** control which protocol wins when both have a release. The default is **Prefer Torrent**: torrents grab immediately, and the direct-download grab only happens if no torrent release turns up within the delay you set. Weekly new issues often exist only on direct-download sources at release time — they still come through, just on the fallback path.

## Multi-issue packs

Releases that bundle many issues ("Vol 4", "#1-31") are detected as packs and **never grabbed automatically** — an automatic search wants one issue, not gigabytes of them. You can still grab a pack deliberately from interactive search; on import, every file inside is identified individually.
