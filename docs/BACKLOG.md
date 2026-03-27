# Panelarr — Product Backlog

**Author:** @pm
**Date:** 2026-03-26
**Source:** PRD v1.0 + ARCHITECTURE v1.0

Stories are ordered by dependency (must be completed top-to-bottom within each phase). Each story is independently deliverable and testable.

---

## Phase 1 — Core (Fork Adaptation)

### Epic 1: Foundation

**P1-01: Fork and rebrand Readarr → Panelarr**
- Fork Readarr repository
- Global find-and-replace: Readarr → Panelarr in solution, project names, namespaces, branding
- Rename `Readarr.Api.V1/` → `Panelarr.Api.V1/`, `Readarr.Http/` → `Panelarr.Http/`
- Update logos, window titles, app name strings
- Verify solution builds and existing tests pass after rename
- **Acceptance:** `dotnet build` succeeds, app launches with Panelarr branding

**P1-02: Define comic-specific enums**
- Create `SeriesStatusType` enum: Continuing, Ended, Cancelled, Hiatus
- Create `SeriesType` enum: Single, TPB, GraphicNovel, Hardcover, Omnibus, OneShot, Annual, Limited
- Create `IssueType` enum: Standard, Annual, Special, OneShot, TPB, Hardcover, Omnibus
- Create `ComicFormat` enum: CBZ, CBR, CB7, PDF, EPUB, Unknown
- **Ref:** ARCHITECTURE.md Section 3.4
- **Acceptance:** Enums compile, used by domain entities in subsequent stories

**P1-03: Create initial database migration**
- Replace Readarr's 40 migration files with a single Panelarr initial migration
- Create tables: Publishers, SeriesMetadata, Series, Issues, ComicFiles, SeriesGroups, SeriesGroupLinks
- Modify inherited tables: NamingConfig (add AnnualIssueFormat, TPBFormat columns)
- Keep all inherited infrastructure tables (Config, Indexers, DownloadClients, etc.)
- **Ref:** ARCHITECTURE.md Section 8.2
- **Acceptance:** App starts with clean database, all tables created, no migration errors on SQLite and PostgreSQL

### Epic 2: Domain Model

**P1-04: Remap core entities — Series and SeriesMetadata**
- Rename `Author` → `Series`, `AuthorMetadata` → `SeriesMetadata`
- Update SeriesMetadata: remove author-specific fields (Gender, Hometown, Born, Died, NameLastFirst, SortNameLastFirst, Aliases), add Year, PublisherId, SeriesType, VolumeNumber, Ratings
- Update Series: rename AuthorMetadataId → SeriesMetadataId, remove MetadataProfileId
- Update all repository classes: `AuthorRepository` → `SeriesRepository`, `AuthorService` → `SeriesService`, etc.
- Update LazyLoaded relationships
- **Ref:** ARCHITECTURE.md Section 3.2
- **Acceptance:** Series and SeriesMetadata compile, repository CRUD operations work

**P1-05: Remap core entities — Issue and ComicFile**
- Rename `Book` → `Issue`, `BookFile` → `ComicFile`
- Update Issue: remove ForeignEditionId, RelatedBooks, AnyEditionOk; add IssueNumber (float), IssueType, CoverArtUrl, PageCount
- Update ComicFile: rename EditionId → IssueId (direct link, no Format layer), remove CalibreId, add ComicFormat, ImageCount, ImageQualityScore
- Remove `Edition` entity and all Edition-related services, repositories, and controllers entirely
- Update all repository/service classes
- **Ref:** ARCHITECTURE.md Section 3.2
- **Acceptance:** Issue and ComicFile compile, Edition code removed, repository CRUD works

**P1-06: Add Publisher entity**
- Create Publisher model: Name, CleanName, ForeignPublisherId, Description, Images
- Create PublisherRepository and PublisherService
- Create PublisherController with `/api/v1/publisher` endpoint
- Wire FK from SeriesMetadata.PublisherId → Publisher.Id
- **Ref:** ARCHITECTURE.md Section 3.3
- **Acceptance:** Publishers can be created/read via API, FK constraint enforced

**P1-07: Remap SeriesGroup and SeriesGroupLink**
- Rename Readarr's `Series` → `SeriesGroup`, `SeriesBookLink` → `SeriesGroupLink`
- Update SeriesGroupLink: references SeriesGroupId and SeriesMetadataId
- Create SeriesGroupController with `/api/v1/seriesgroup` endpoint
- SeriesGroup is user-created, optional soft-tag — no auto-inference
- **Ref:** ARCHITECTURE.md Sections 3.1, 9.1
- **Acceptance:** SeriesGroups can be created, Series can be linked to groups, API endpoints functional

### Epic 3: Metadata Provider

**P1-08: Define IMetadataProvider interface**
- Create `IMetadataProvider` interface with methods: SearchSeries, GetSeriesInfo, GetChangedSeries, GetIssues, GetIssueInfo, GetPublisher, GetNewReleases
- Create provider DTOs: ProviderSeries, ProviderIssue, ProviderPublisher
- Remove all Goodreads/BookInfo proxy code and references
- **Ref:** ARCHITECTURE.md Section 4.2
- **Acceptance:** Interface compiles, old metadata code removed, DI registration placeholder exists

**P1-09: Implement MetronProvider**
- Implement `MetronProvider : IMetadataProvider`
- Create `MetronApiClient` with Basic auth, token bucket rate limiter (20 req/min, 5K req/day)
- Implement API calls: series search, series detail, issue list, issue detail, publisher detail
- Aggressive caching: series 24h, issues 12h via `ICachedHttpResponseService`
- Bulk issue fetch via paginated list call (not per-issue)
- **Ref:** ARCHITECTURE.md Section 4.3
- **Acceptance:** Can search Metron for "Batman", get series details, list issues. Rate limiter prevents exceeding limits.

**P1-10: Implement MetronMapper and wire integration**
- Create `MetronMapper`: ProviderSeries → SeriesMetadata + Series, ProviderIssue → Issue, ProviderPublisher → Publisher
- Enrich mapped results with local DB IDs (existing series detection)
- Wire MetronProvider into DI container as primary IMetadataProvider
- **Ref:** ARCHITECTURE.md Section 4.4
- **Acceptance:** Adding a series from Metron search creates correct Series, SeriesMetadata, Publisher, and Issue records in database

### Epic 4: Release Parser

**P1-11: Implement comic release name parser**
- Create `ParsedComicInfo` model: SeriesTitle, IssueNumber, VolumeNumber, Year, IssueType, TotalIssues, ComicFormat, ReleaseGroup, Quality, Source
- Implement comic-specific regex patterns: issue number, volume, year, annual/special/TPB markers, source, release group
- Implement parsing pipeline: extension → release group → source → year → issue number → volume → markers → series title
- **Ref:** ARCHITECTURE.md Section 5.2, 5.3
- **Acceptance:** Parser correctly handles example release names from ARCHITECTURE.md Section 5.1. Test suite covers common naming patterns.

**P1-12: Update ParsingService for comic matching**
- Update `ParsingService` to use `ParsedComicInfo` instead of `ParsedBookInfo`
- Implement fuzzy matching: parsed series title → known Series in database
- Implement issue matching: parsed issue number → known Issue
- Wire parser into indexer result processing and import pipeline
- **Acceptance:** Indexer search results are correctly parsed, matched to monitored series/issues

### Epic 5: File Organizer

**P1-13: Implement comic naming tokens and per-IssueType templates**
- Update `FileNameBuilder` with comic tokens: {Series Title}, {Series Year}, {Publisher}, {Issue Number}, {Issue Title}, {Issue Type}, {Release Year}, etc.
- Add `{Issue Type}` as a folder token for subfolder separation
- Update `NamingConfig` with three format templates: StandardIssueFormat, AnnualIssueFormat, TPBFormat
- Set defaults: `{Series Title} ({Series Year}) #{Issue Number:000}` (standard), `{Series Title} ({Series Year}) Annual #{Issue Number:000}` (annual), `{Series Title} ({Series Year}) Vol {Issue Number:00} TPB` (TPB)
- Default folder format: `{Publisher}/{Series Title} ({Series Year})`
- **Ref:** ARCHITECTURE.md Sections 6.2, 6.3, 6.4
- **Acceptance:** Files renamed correctly for each IssueType. Naming preview in settings UI shows correct output.

### Epic 6: ComicInfo.xml

**P1-14: Implement ComicInfo.xml generation and embedding**
- Implement ComicInfo.xml generator from Issue + SeriesMetadata + Publisher
- Include fields: Series, Number, Year, Publisher, PageCount, Title, Genre, Summary
- Embed ComicInfo.xml inside CBZ files on import/rename
- Skip embedding for non-CBZ formats (CBR, PDF, EPUB) — log info message
- **Ref:** ARCHITECTURE.md Section 12, Step 5.5
- **Acceptance:** Imported CBZ files contain valid ComicInfo.xml. Kavita/Komga can read the metadata.

### Epic 7: Quality System

**P1-15: Define comic quality definitions**
- Replace Readarr's book/audio quality definitions with comic qualities: Unknown, PDF, CBR, CBZ, CB7, EPUB, CBZ_HD, CBZ_Web
- Create default quality profile: cutoff CBZ, preferred order CBZ_HD > CBZ > CB7 > CBR > PDF > EPUB > CBZ_Web
- Add comic-specific custom format conditions: file extension, source (Digital/Scan/c2c), release group, tag matching (noads, digital)
- **Ref:** ARCHITECTURE.md Section 7
- **Acceptance:** Quality profile UI shows comic formats. Release matching applies correct quality.

### Epic 8: API & Frontend

**P1-16: Rename API controllers and resources**
- Rename controllers: AuthorController → SeriesController, BookController → IssueController, BookFileController → ComicFileController, BookShelfController → MonitorController
- Remove EditionController entirely
- Rename all Resource classes and their mappers
- Update all endpoint URLs: `/api/v1/author` → `/api/v1/series`, etc.
- Update SignalR hub registrations
- **Acceptance:** All API endpoints respond correctly with renamed resources. No 404s on new URLs.

**P1-17: Adapt frontend for comics**
- Rename frontend directories: Author/ → Series/, Book/ → Issue/
- Rename Redux actions and reducers: authorActions → seriesActions, bookActions → issueActions
- Rename all UI labels: Author → Series, Book → Issue
- Add IssueNumber, Publisher, SeriesType fields to forms and detail views
- Remove Calibre integration and audiobook-specific UI elements
- Rebrand: Panelarr logos, titles, accent colors
- **Acceptance:** UI shows series list, series detail with issues, add series dialog, settings pages. All labels say Panelarr, not Readarr.

### Epic 9: Infrastructure Verification

**P1-18: Verify inherited infrastructure**
- Verify indexer search works (Newznab/Torznab with comic parser)
- Verify download client grab works (SABnzbd and/or qBittorrent)
- Verify completed download import pipeline (download → parse → rename → move)
- Verify authentication modes: None, Basic, Forms, External
- Verify health checks report correct status
- Verify SQLite and PostgreSQL both work
- **Acceptance:** End-to-end flow: search → grab → download → import → correctly named file in library

**P1-19: Docker packaging**
- Create Dockerfile for Panelarr (based on Readarr's Dockerfile)
- Create docker-compose.yml example
- Verify PUID/PGID support
- Test container startup, volume mounts, port mapping
- **Acceptance:** `docker-compose up` starts Panelarr, accessible on configured port, persists data across restarts

---

## Phase 2 — Library & Enrichment

**P2-01: Import existing local comic files**
- Scan folder for comic files (CBZ, CBR, CB7, PDF, EPUB)
- Parse filenames and match to Metron metadata
- Rename and move matched files to library folder
- Handle unmatched files: quarantine or manual match UI
- **Acceptance:** User points to a folder of comics, files are matched, renamed, and organized

**P2-02: CBZ/CBR archive inspection**
- Open CBZ/CBR archives via SharpCompress
- Extract page count, image dimensions, image quality scoring
- Populate ComicFile.ImageCount and ComicFile.ImageQualityScore
- Handle corrupt/unreadable archives gracefully (log warning, skip inspection)
- **Acceptance:** Imported files show page count and quality score in UI

**P2-03: Duplicate detection and resolution**
- Detect when multiple ComicFiles exist for the same Issue
- Compare quality scores, prefer higher quality
- UI for manual resolution of conflicts
- **Acceptance:** Duplicates flagged in UI, auto-resolution picks best quality

**P2-04: Manual metadata override**
- Allow user to manually edit Series/Issue metadata (title, issue number, year, publisher, type)
- Store overrides locally — don't lose on metadata refresh
- **Acceptance:** Edited metadata persists across refreshes

**P2-05: Bulk operations**
- Monitor/unmonitor multiple issues or entire series in one action
- Bulk delete, re-scan, search
- **Acceptance:** User can select multiple items and apply bulk actions

**P2-06: Add ComicVine as secondary metadata provider**
- Implement `ComicVineProvider : IMetadataProvider`
- User provides their own API key in settings
- Fallback logic: try Metron first, fall back to ComicVine on miss
- **Acceptance:** Series not found in Metron can be found via ComicVine

**P2-07: Enhanced library UI**
- Library overview with statistics (total series, issues, missing, file size)
- Missing issues view across all series
- Calendar view for upcoming releases
- SeriesGroup filtering in library view
- Issue history and event log
- **Acceptance:** All views render correctly with live data

**P2-08: Custom format scoring and upgrade logic**
- Comic-specific custom format conditions: image resolution, source, release group, page count
- Upgrade logic: auto-replace lower-quality files when better versions found
- **Acceptance:** Better quality release auto-replaces existing file

---

## Phase 3 — Automation & Ecosystem

**P3-01: RSS sync and scheduled search**
- RSS sync with configurable interval
- Scheduled background search for all missing issues
- Auto-unmonitor after download (configurable per series)
- **Acceptance:** Panelarr runs unattended, grabs new releases, fills missing issues on schedule

**P3-02: Release blacklisting**
- Ignore releases by name pattern, indexer, or release group
- UI for managing blacklist rules
- **Acceptance:** Blacklisted releases are skipped during search

**P3-03: Notification templates**
- Verify/adapt inherited notification integrations
- Comic-specific templates: "New issue grabbed: Batman #42", "Import complete", etc.
- **Acceptance:** Notifications fire correctly with comic-specific messages

**P3-04: REST API documentation**
- Full API audit — every UI action backed by endpoint
- OpenAPI / Swagger documentation
- API changelog and versioning
- **Acceptance:** Swagger UI accessible, all endpoints documented

**P3-05: Reader integration — Kavita and Komga**
- Kavita API: trigger library scan on import
- Komga API: trigger library scan on import
- MetronInfo.xml support alongside ComicInfo.xml
- **Acceptance:** New imports automatically appear in connected reader

**P3-06: Production Docker images**
- Multi-arch builds: amd64, arm64
- docker-compose example with Prowlarr + SABnzbd + Panelarr stack
- Helm chart (stretch)
- Unraid Community Apps template (stretch)
- **Acceptance:** Docker images published, compose stack runs end-to-end

---

## Future — Post-Phase 3

**F-01: Story Arcs**
- StoryArc entity with ForeignStoryArcId, Title, Description
- StoryArcIssueLink join table with ReadingOrder position
- "Add story arc" flow: monitor/download issues across multiple Series
- Embed story arc info in ComicInfo.xml
- Import/export reading lists
- **Ref:** ARCHITECTURE.md Section 16.1

---

## Story Status Key

- **Ready** — fully specified, can be picked up
- **In Progress** — actively being worked on
- **Done** — complete and verified
- **Blocked** — waiting on dependency or decision

| Story | Status | Depends On |
|---|---|---|
| P1-01 | Done | — |
| P1-02 | Done | — |
| P1-03 | Done | P1-02 |
| P1-04 | Done | P1-01, P1-03 |
| P1-05 | Done | P1-04 |
| P1-06 | Done | P1-04 |
| P1-07 | Done | P1-04 |
| P1-08 | Done | P1-05 |
| P1-09 | Done | P1-08 |
| P1-10 | Done | P1-09, P1-06 |
| P1-11 | Done | P1-05 |
| P1-12 | Done | P1-11 |
| P1-13 | Done | P1-05, P1-06 |
| P1-14 | Done | P1-13 |
| P1-15 | Done | P1-05 |
| P1-16 | Done | P1-05, P1-06, P1-07 |
| P1-17 | Done | P1-16 |
| P1-18 | Done | P1-12, P1-13, P1-15, P1-17 |
| P1-19 | Done | P1-18 |
| P2-01 | Ready | Phase 1 complete |
| P2-02 | Ready | P2-01 |
| P2-03 | Ready | P2-02 |
| P2-04 | Ready | Phase 1 complete |
| P2-05 | Ready | Phase 1 complete |
| P2-06 | Ready | Phase 1 complete |
| P2-07 | Ready | Phase 1 complete |
| P2-08 | Ready | P2-02 |
| P3-01 | Ready | Phase 2 complete |
| P3-02 | Ready | Phase 2 complete |
| P3-03 | Ready | Phase 2 complete |
| P3-04 | Ready | Phase 2 complete |
| P3-05 | Ready | P1-14, Phase 2 complete |
| P3-06 | Ready | Phase 3 features complete |
| F-01 | Ready | Phase 3 complete |
