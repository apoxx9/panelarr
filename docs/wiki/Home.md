Welcome to the **Panelarr** wiki. Panelarr is a self-hosted comic book manager in the *arr family: it monitors series, discovers new issues, searches your indexers, sends grabs to your download client, and organizes the files — with comic-native metadata all the way down.

## Getting started

The [README](https://github.com/apoxx9/panelarr#quick-start-docker) covers installation (Docker and from source). First steps after install:

1. **Settings → Metadata** — add your Metron and/or ComicVine API keys.
2. **Settings → Indexers / Download Clients** — hook up Prowlarr (or indexers directly) and your download client.
3. **Library → Add New** — start tracking series, or use **Library Import** to bring an existing collection in from disk.

## Feature guides

- [[Reading Lists]] — story arcs, community CBL files, and pushing lists to your reader
- [[Pull List]] — the weekly new-comic-book-day view
- [[Library Import]] — importing an existing collection and the staging-folder workflow
- [[Reader Integration]] — Kavita and Komga connections
- [[Direct Downloads]] — the GetComics client, mirrors, and torrent-first delay profiles
- [[Tagging and Conversion]] — ComicInfo/MetronInfo embedding, retagging, CBZ conversion

## The comic domain model

Panelarr maps **Publisher → Series → Issue → ComicFile**. A series is 1:1 with a ComicVine volume; annuals and specials are separate series that can be linked to their parent via **related series** (a display-only link — matching and file management stay unambiguous). Naming templates are per-type (standard issues, annuals, TPBs), and ComicInfo.xml / MetronInfo.xml metadata is embedded into CBZ files so readers like Kavita pick everything up.
