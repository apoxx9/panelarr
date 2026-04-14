<p align="center">
  <img src="Logo/256.png" alt="Panelarr" width="128" />
</p>

<h1 align="center">Panelarr</h1>

<p align="center">
  Comic book management and download automation for the <em>*arr</em> ecosystem.
</p>

---

Panelarr is a self-hosted comic book manager that monitors series, discovers new issues, searches indexers, sends grabs to download clients, and organizes your library automatically. It gives comic collectors the same experience that [Sonarr](https://sonarr.tv) provides for TV and [Radarr](https://radarr.video) for movies.

Forked from [Readarr](https://github.com/Readarr/Readarr) (GPL v3) with a fully reworked domain model built for comics.

## Features

- **Full *arr integration** — works with Prowlarr, SABnzbd, NZBGet, qBittorrent, Deluge, Transmission, and other download clients
- **Comic-aware data model** — Publisher > Series > Issue > ComicFile (no shoehorning comics into book/audiobook models)
- **Metadata providers** — Metron (primary) and ComicVine (fallback) for series, issue, and publisher metadata
- **Smart file handling** — supports CBZ, CBR, CB7, PDF, and EPUB formats with automatic quality scoring
- **ComicInfo.xml & MetronInfo.xml** — embeds metadata into CBZ files for seamless integration with Kavita, Komga, and other readers
- **Per-type naming** — separate naming templates for standard issues, annuals, and TPBs
- **Quality profiles** — automatic upgrades when better quality releases are found
- **Reader integration** — triggers library scans in Kavita and Komga on import
- **Beautiful UI** — React-based web interface following *arr conventions

## Quick Start (Docker)

```yaml
services:
  panelarr:
    image: ghcr.io/apoxx9/panelarr:latest
    container_name: panelarr
    environment:
      - PUID=1000
      - PGID=1000
      - TZ=Etc/UTC
    volumes:
      - ./config:/config
      - ./comics:/comics
      - ./downloads:/downloads
    ports:
      - "8787:8787"
    restart: unless-stopped
```

A full stack `docker-compose.yml` (with Prowlarr, SABnzbd, and qBittorrent) is included in the repo.

## Building from Source

**Requirements:** .NET 10.0 SDK, Node.js 20+, Yarn

```bash
# Backend
cd src
dotnet build Panelarr.sln

# Frontend
cd frontend
yarn install
yarn build

# Run
dotnet run --project NzbDrone.Console --framework net10.0
```

The app will be available at `http://localhost:8787`.

## Architecture

Panelarr inherits the full *arr infrastructure from Readarr — indexer protocols, download client APIs, quality profiles, release parsing, job scheduling, SignalR real-time updates, and more — with a comic-specific domain layer on top.

The domain model maps Publisher > Series > Issue > ComicFile, replacing Readarr's Author > Book > Edition hierarchy with comic-native entities.

## Supported Platforms

- Linux (amd64, arm64, armv7)
- macOS
- Windows
- Docker

## License

[GNU GPL v3](LICENSE.md)
