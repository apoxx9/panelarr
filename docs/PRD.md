# Panelarr — Product Requirements Document

**Version:** 1.0
**Author:** @analyst
**Status:** Draft
**Date:** 2026-03-26

---

## 1. Executive Summary

Panelarr is a self-hosted comic book management and download automation tool, forked from [Readarr](https://github.com/Readarr/Readarr) (GPL v3, C# / .NET) and adapted for comics. It monitors comic series, discovers new issues, searches configured indexers, sends grabs to download clients, and organises the local library automatically — giving self-hosters the same *arr experience for comics that Sonarr provides for TV and Radarr for movies.

**The gap Panelarr fills:** No existing comic tool combines the full *arr automation stack (indexers → download clients → quality profiles → library management) with a clean, comic-aware data model. Mylar3 has indexer support but is aging, fragile, and plagued by data model bugs. Kapowarr has a modern UI but only supports GetComics as a source — no usenet, no torrent indexers, no Prowlarr integration. Panelarr inherits the full *arr plumbing from Readarr and builds comic-specific intelligence on top.

---

## 2. Target Users

Self-hosters familiar with the *arr ecosystem (Sonarr, Radarr, Prowlarr, etc.) who want the same automated workflow for comic books. These users:

- Run Docker-based media stacks
- Already use Prowlarr for indexer management
- Use download clients like SABnzbd, NZBGet, qBittorrent, Deluge, Transmission
- Expect a web UI with *arr-convention UX patterns
- May use comic readers like Kavita or Komga for consumption

---

## 3. Fork Decision — Resolved

### Why Readarr (not Sonarr, not Kapowarr, not from scratch)

| Option | Verdict | Rationale |
|---|---|---|
| **Readarr** | **Selected** | Two-level data model (Author → Book ≈ Series → Issue). Full *arr stack inherited: indexers, download clients, quality profiles, job scheduler, notifications, React UI, API, SQLite + PostgreSQL. GPL v3. |
| Sonarr | Rejected | Three-level model (Show → Season → Episode) has an intermediate level (Season) that doesn't exist in comics. Would require more invasive model surgery. |
| Kapowarr | Rejected | Python/Flask, only supports GetComics as download source. Adding the full *arr indexer/download client stack would be equivalent effort to the Readarr fork, but with a hand-rolled stack instead of a battle-tested one. |
| From scratch | Rejected | The *arr infrastructure (indexer protocols, download client APIs, quality profiles, release parsing, job scheduling, SignalR, etc.) is thousands of lines of battle-tested code. Rebuilding it would dominate the project timeline. |

### What We Inherit for Free

- Newznab / Torznab indexer protocol support
- Download client integrations (SABnzbd, NZBGet, qBittorrent, Deluge, Transmission, etc.)
- Quality profile and custom format system
- Background job scheduler
- Notification system (email, Slack, Discord, Telegram, Gotify, Ntfy, webhooks, etc.)
- React-based web UI shell
- REST API with consistent *arr conventions
- Authentication (None, Basic, Forms, External/proxy)
- SQLite (default) + PostgreSQL support
- Docker packaging
- SignalR real-time updates
- Health check system
- Log management with log database

---

## 4. Domain Model

### 4.1 Core Entity Mapping (Readarr → Panelarr)

| Readarr Entity | Panelarr Entity | Source |
|---|---|---|
| Author | **Series** | Metadata provider (CV Volume) |
| Book | **Issue** | Metadata provider (CV Issue) |
| Edition | **Format** | File inspection (CBZ, CBR, PDF, EPUB) |
| — | **Publisher** | Metadata provider |
| — | **SeriesGroup** | User-defined soft-tag (optional) |

### 4.2 Entity Definitions

**Publisher**
- Name, description, logo URL
- Sourced from metadata provider
- Used in folder organization: `{Root}/{Publisher}/{Series}/`

**SeriesGroup** (Phase 1: soft-tag only)
- Optional user-defined label grouping related Series
- Example: "Batman" groups Batman (1940), Batman (2011), Batman (2016)
- No automatic inference — user-created and user-managed
- Used for UI filtering/grouping, not for file organization
- Can be promoted to a first-class entity in a future phase if demand warrants

**Series**
- Maps to a metadata provider Volume (one per run/relaunch)
- Fields: title, year, publisher, status, type, volume_number, description, cover_art_url, metadata_provider_id
- `status` enum: `Ongoing | Complete | Cancelled | Hiatus`
- `type` enum: `Single | TPB | GN | HC | Omnibus | OneShot | Annual | Limited`
- Phase 1 implements `Single` type workflows only; other types are schema-ready but not active
- Always stores metadata_provider_id to avoid ambiguity (prevents Mylar's same-name-same-year merging bug)

**Issue**
- Maps to a metadata provider Issue
- Fields: issue_number, title, release_date, description, cover_art_url, page_count, price, isbn, metadata_provider_id
- `monitored` flag: whether Panelarr should search for this issue
- `grabbed` / `downloaded` / `missing` status tracking

**Format** (file-level)
- Represents a physical/digital file for an Issue
- Fields: file_path, format (CBZ, CBR, PDF, EPUB), file_size, quality_score
- One Issue can have multiple Formats (e.g., CBZ and PDF)

### 4.3 File Organization

Default naming scheme:
```
{RootFolder}/{Publisher}/{Series} ({Year})/{Series} ({Year}) #{IssueNumber:000}.{ext}
```

Examples:
```
/comics/DC Comics/Batman (2016)/Batman (2016) #001.cbz
/comics/Marvel/Amazing Spider-Man (2022)/Amazing Spider-Man (2022) #015.cbz
/comics/Image/Saga (2012)/Saga (2012) #066.cbz
```

Key rules:
- Metadata provider ID is stored in the database, NOT encoded in the folder path (keeps paths human-readable)
- Issue number zero-padded to 3 digits by default (configurable)
- Year in folder name provides volume disambiguation at the filesystem level
- Publisher folder is optional (configurable)
- Custom naming tokens available: `{Series}`, `{Year}`, `{Publisher}`, `{IssueNumber}`, `{IssueTitle}`, `{Type}`, `{Format}`

### 4.4 Comic-Specific Complexity

**Annuals and Specials:**
- Stored as Issues with a flag or distinct numbering scheme
- Follow Readarr's pattern for handling "extra" content types
- Naming: `{Series} ({Year}) Annual #{Number}.cbz`

**Crossover Issues:**
- An issue belongs to exactly one Series (its primary series)
- Story arc / crossover tracking is a future feature (not Phase 1)

**Variant Covers:**
- Not tracked in Phase 1
- Schema allows for a `covers[]` array on Issue for future use

---

## 5. Metadata Provider Architecture

### 5.1 Design Principle

Panelarr defines an `IMetadataProvider` interface. All metadata operations go through this abstraction. The domain model stores Panelarr's own entities — never raw provider data. This decouples Panelarr from any single metadata source.

### 5.2 Provider Roadmap

| Phase | Provider | Role | Notes |
|---|---|---|---|
| Phase 1 | **Metron** | Primary | Best data model for comics. 10 explicit series types, ~112K issues, GPLv3 server, open license. REST API, Basic auth. User registers for free account at metron.cloud. Rate limit: 20 req/min, 5K req/day. Python client library (Mokkari) available as reference. |
| Phase 2 | **ComicVine** | Secondary/Fallback | Largest consumer DB (~507K issues). Fills coverage gaps where Metron lacks data. User provides their own API key. Non-commercial license — used as supplementary source only. |
| Future | **ComicDB** | Primary (replaces Metron) | Our own metadata service (separate project). GCD-seeded, community-curated, purpose-built for Panelarr. Eliminates third-party dependency. |

### 5.3 IMetadataProvider Interface (Conceptual)

```
SearchSeries(query, year?, publisher?) → Series[]
GetSeries(providerId) → Series
GetIssues(seriesProviderId) → Issue[]
GetIssue(issueProviderId) → Issue
GetCoverArt(issueProviderId) → URL
GetNewReleases(date) → Issue[]
```

### 5.4 Metadata Refresh

- On series add: full pull of series + all issues
- Scheduled refresh: daily check for new issues on monitored series
- Manual refresh: user-triggered full re-sync of a series
- Metadata is cached locally — provider unavailability does not break the library

---

## 6. Feature Scope — Phased

### Phase 1 — Core (Fork Adaptation)

**Goal:** A working comic *arr app. Add a series, search for issues, download them, organize the library.

**Domain Model:**
- [ ] Remap Readarr entities: Author → Series, Book → Issue, Edition → Format
- [ ] Add Publisher entity
- [ ] Add SeriesGroup as optional soft-tag
- [ ] Add `type` enum to Series (Single | TPB | GN | HC | Omnibus | OneShot | Annual | Limited) — only `Single` workflows active
- [ ] Implement comic-specific file naming: `{Root}/{Publisher}/{Series} ({Year})/{Series} ({Year}) #{IssueNumber:000}.{ext}`

**Metadata Provider:**
- [ ] Define `IMetadataProvider` interface
- [ ] Implement Metron provider (primary)
- [ ] Series search, issue listing, cover art retrieval
- [ ] Local metadata cache with scheduled refresh

**Core Workflows:**
- [ ] Add and monitor a comic series
- [ ] View missing issues for monitored series
- [ ] Manual search: trigger indexer search for a specific issue or all missing issues in a series
- [ ] Automatic search: RSS sync discovers new releases and grabs monitored issues
- [ ] Send grabs to configured download client
- [ ] Import completed downloads: rename, move to library folder
- [ ] Monitor individual issues (mark specific issues as wanted/unwanted)
- [ ] Unmonitor/remove series

**Release Parsing:**
- [ ] Comic-specific release name parsing (handle issue numbers, annual designators, publisher names, format indicators)
- [ ] Quality detection from release names (CBZ vs CBR vs PDF, resolution hints)

**UI (minimal adaptation of Readarr UI):**
- [ ] Series list (grid + list view)
- [ ] Series detail page (issue list with status indicators)
- [ ] Add series dialog (search + select)
- [ ] Activity queue (current downloads, history)
- [ ] Settings pages (indexers, download clients, naming, metadata provider, general)
- [ ] System pages (status, logs, updates)

**Infrastructure (inherited, verify working):**
- [ ] SQLite default, PostgreSQL optional
- [ ] Authentication: None, Basic, Forms, External
- [ ] API key auth for REST API
- [ ] Docker image + docker-compose example
- [ ] Health checks

### Phase 2 — Library & Enrichment

**Goal:** Rich library management, import existing collections, TPB support, better metadata.

**Library Management:**
- [ ] Import existing local comic files into managed library (scan folder, match to metadata, rename/move)
- [ ] CBZ/CBR archive inspection (page count, image dimensions, image quality scoring)
- [ ] Duplicate detection and resolution
- [ ] Manual metadata override per series/issue
- [ ] Bulk operations (monitor/unmonitor, delete, re-scan)

**TPB / Collected Editions:**
- [ ] Activate TPB, GN, HC, Omnibus, OneShot workflows
- [ ] Separate naming template for collected editions: `{Series} ({Year}) Vol. {VolumeNumber}.{ext}`
- [ ] Book type detection heuristics (page count > 100, ISBN present, price signals)
- [ ] Prevent cross-matching (don't match TPB downloads against single-issue wants, and vice versa)

**Metadata Enrichment:**
- [ ] Add ComicVine as secondary metadata provider (user provides API key)
- [ ] Cross-reference providers for better coverage
- [ ] Rich metadata display: description, creators, publisher, release schedule
- [ ] Cover art caching and display

**UI Enhancements:**
- [ ] Library overview with statistics (total series, issues, missing, file size)
- [ ] Missing issues view (across all series)
- [ ] Recent activity feed
- [ ] Calendar view (upcoming releases)
- [ ] SeriesGroup filtering/organization in library view
- [ ] Issue history and event log

**File Quality:**
- [ ] Custom format scoring (prefer CBZ over CBR, minimum page count, image resolution thresholds)
- [ ] Upgrade logic: automatically replace lower-quality files when better versions are found

### Phase 3 — Automation & Ecosystem

**Goal:** Full automation, ecosystem integration, production-ready packaging.

**Automation:**
- [ ] RSS sync with configurable interval
- [ ] Scheduled background search for missing issues
- [ ] Release blacklisting (ignore specific releases by name pattern, indexer, etc.)
- [ ] Auto-unmonitor after download (configurable per series)

**Notifications:**
- [ ] Verify/adapt inherited notification integrations: email, Slack, Discord, Telegram, Gotify, Ntfy, Apprise, webhooks
- [ ] Comic-specific notification templates ("New issue grabbed: Batman #42")

**API:**
- [ ] Full REST API audit — ensure every UI action is backed by an endpoint
- [ ] OpenAPI / Swagger documentation
- [ ] API changelog and versioning

**Reader Integration:**
- [ ] Kavita integration (API-based library refresh trigger)
- [ ] Komga integration (API-based library refresh trigger)
- [ ] ComicInfo.xml / MetronInfo.xml generation inside CBZ files

**Packaging:**
- [ ] Production Docker image (multi-arch: amd64, arm64)
- [ ] docker-compose example with common stack (Prowlarr, SABnzbd, Panelarr)
- [ ] Helm chart (stretch goal)
- [ ] Unraid Community Apps template (stretch goal)

---

## 7. Hard Constraints

1. **API-first:** Every UI action must be backed by a REST endpoint. The UI is a consumer of the API, not a separate system.
2. **Configuration:** Via `config.xml` (inherited from Readarr) and environment variables. Support Docker-standard `PUID`/`PGID` for permissions.
3. **Must run in Docker:** Docker is the primary deployment target. Bare-metal is supported but not the priority.
4. **Graceful degradation:** Metadata provider unavailability must not crash the app or break the existing library. Indexer and download client failures must be reported via health checks, not unhandled exceptions.
5. **Stay close to *arr UX conventions:** Users familiar with Sonarr/Radarr/Readarr should feel immediately at home. Don't reinvent interaction patterns.
6. **Metadata provider ID always stored:** Every Series and Issue stores its metadata provider ID in the database. Folder paths remain human-readable (no IDs in paths), but the database link prevents the same-name-same-year merging bug that plagues Mylar3.
7. **No hardwired metadata source:** All metadata access goes through `IMetadataProvider`. Switching or adding providers must not require domain model changes.
8. **GPL v3 license:** Inherited from Readarr fork. All Panelarr code is open source under GPL v3.

---

## 8. Anti-Patterns to Avoid

Lessons learned from studying Mylar3 and Kapowarr:

| Anti-Pattern | Source | How Panelarr Avoids It |
|---|---|---|
| Merging series with same name + year, causing data loss | Mylar3 (issue #2322) | Always key on metadata provider ID, never on name + year alone |
| Guessing book type from description text heuristics | Mylar3, Kapowarr | Use Metron's explicit series types. When unavailable, use multiple signals (page count, price, ISBN), not description parsing. Allow manual override with prominent UI. |
| Single metadata source as sole dependency | Mylar3, Kapowarr (both ComicVine-only) | `IMetadataProvider` abstraction. Metron primary, ComicVine fallback, ComicDB future. Local cache ensures offline resilience. |
| Non-commercial API license risk | ComicVine ToS | Metron (GPLv3) as primary. ComicVine as user-optional secondary only. |
| TPBs and singles sharing the same matching/naming logic | Mylar3 | Separate workflows, separate naming templates, no cross-matching |
| Flat data model with no way to relate volumes | Mylar3, Kapowarr | SeriesGroup soft-tag from Phase 1. Promotable to first-class entity later. |
| Fragile folder format leading to collisions | Mylar3 | Year in folder name for disambiguation. Provider ID in database for authoritative identity. |

---

## 9. Reference Projects

| Project | Role | URL |
|---|---|---|
| Readarr | Fork base | https://github.com/Readarr/Readarr |
| Sonarr | UX/API conventions reference | https://github.com/Sonarr/Sonarr |
| Radarr | UX/API conventions reference | https://github.com/Radarr/Radarr |
| Mylar3 | Anti-pattern study, feature reference | https://github.com/mylar3/mylar3 |
| Kapowarr | Anti-pattern study, UI reference | https://github.com/Casvt/Kapowarr |
| Metron | Primary metadata provider | https://metron.cloud/ |
| ComicVine | Secondary metadata provider | https://comicvine.gamespot.com/api/ |
| GCD | Future data source (CC BY-SA 4.0 dumps) | https://www.comics.org/ |
| Kavita | Reader integration target | https://github.com/Kareadita/Kavita |
| Komga | Reader integration target | https://github.com/gotson/komga |

---

## 10. Success Criteria

### Phase 1 Complete When:
- A user can add a comic series by searching Metron
- Missing issues are identified and displayed
- Manual and automatic search finds releases via configured indexers
- Grabs are sent to a download client
- Completed downloads are renamed and moved to the library with correct folder/file naming
- Web UI provides series list, series detail, activity queue, and settings
- Docker image is published and functional

### Phase 2 Complete When:
- Existing comic libraries can be imported and matched to metadata
- TPBs can be monitored and downloaded separately from single issues
- ComicVine is available as a fallback metadata provider
- Library overview shows comprehensive statistics and missing issue tracking

### Phase 3 Complete When:
- Full automation runs unattended (RSS sync, scheduled search, notifications)
- REST API is fully documented with OpenAPI/Swagger
- Reader integration triggers library refreshes in Kavita/Komga
- ComicInfo.xml metadata is written to CBZ files
- Production-grade Docker images are published for amd64 and arm64

---

## 11. Open Questions for @architect

1. **Release parsing complexity:** Comic release names are less standardized than TV/movie. How much of Readarr's release parser can be reused vs. needs rewriting? Need to analyze `Parser/` directory in Readarr source.
2. **Metron rate limits:** 20 req/min, 5K req/day is tight for initial series adds with many issues. What caching/batching strategy do we need?
3. **CBZ inspection:** What library handles CBZ/CBR archive reading in .NET? SharpCompress? Do we need to support RAR5?
4. **Database migration path:** How do we handle the Readarr → Panelarr schema migration? Clean migration on first run, or new database?
5. **UI rebranding scope:** How deep does the Readarr UI adaptation need to go in Phase 1? Reskin only, or structural component changes?

---

## Appendix A: Competitive Landscape

| Feature | Mylar3 | Kapowarr | Panelarr (planned) |
|---|---|---|---|
| Usenet indexers | Yes | No | Yes (inherited) |
| Torrent indexers | Partial | GetComics torrents only | Yes (inherited) |
| Prowlarr integration | Yes | No | Yes (inherited) |
| Download clients | SABnzbd, NZBGet, torrents | Mega, MediaFire, DDL | SABnzbd, NZBGet, qBit, Deluge, Transmission (inherited) |
| Metadata source | ComicVine only | ComicVine only | Metron primary, ComicVine fallback, extensible |
| Series types (TPB, HC, etc.) | Heuristic guess | Auto-detect with manual override | Metron-sourced, explicit, with fallback heuristics |
| Series grouping | None | None | SeriesGroup soft-tag |
| Quality profiles | No | No | Yes (inherited) |
| Custom formats | No | No | Yes (inherited) |
| Notifications | Limited | Limited | Full *arr notification stack |
| PostgreSQL support | No | No | Yes (inherited) |
| REST API | Partial | Partial | Full, OpenAPI-documented |
| Tech stack | Python | Python/Flask | C# / .NET |
| License | GPL v3 | GPL v3 | GPL v3 |
| Status | Aging, slow development | Active, solo dev | Planned |

## Appendix B: Metadata Provider Comparison

| | ComicVine | Metron | GCD |
|---|---|---|---|
| Total issues | ~507K | ~112K | ~2.18M |
| Series types | None | 10 explicit types | None (bibliographic) |
| Series grouping | No | No | No |
| API stability | Stable | Stable | Unstable |
| License | Non-commercial only | GPLv3, open | CC BY-SA 4.0 |
| Rate limits | 200/hr/resource | 20/min, 5K/day | Unknown |
| International | Weak | US-focused | 108 countries |
| Community | Declining | Small, growing | Established nonprofit |
| Booktype field | No | Yes (first-class) | No |
