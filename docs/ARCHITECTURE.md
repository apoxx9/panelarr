# Panelarr — Architecture Document

**Version:** 1.0
**Author:** @architect
**Status:** Draft
**Date:** 2026-03-26
**Based on:** Readarr source analysis (commit HEAD, develop branch)

---

## 1. Overview

Panelarr is forked from Readarr. This document maps Readarr's architecture to Panelarr's requirements, identifies what we keep, what we modify, and what we replace. The goal is to minimise changes to inherited infrastructure while fully adapting the domain layer for comics.

### Change Classification

| Category | Strategy | Scope |
|---|---|---|
| **Keep as-is** | No changes needed | Download clients, indexers, notifications, auth, job scheduler, SignalR, health checks, config system |
| **Rename only** | Find-and-replace with minor adjustments | API resources, frontend components, branding |
| **Modify** | Structural changes to existing code | Domain model, organizer tokens, quality definitions, database schema |
| **Replace** | Remove and rewrite | Metadata provider, release parser, metadata profiles |

---

## 2. Solution Structure

### Readarr Project Layout → Panelarr

```
src/
├── NzbDrone.Common/            # KEEP — shared utilities, HTTP, disk, crypto, caching
├── NzbDrone.Core/              # MODIFY — domain model, parser, organizer, metadata
│   ├── Books/                  #   → rename to Comics/ — domain entities
│   ├── MetadataSource/         #   → REPLACE — Metron provider instead of BookInfo/Goodreads
│   ├── Parser/                 #   → REPLACE — comic release name parsing
│   ├── Organizer/              #   → MODIFY — comic-specific naming tokens
│   ├── MediaFiles/             #   → MODIFY — BookFile → ComicFile, CBZ/CBR handling
│   ├── Qualities/              #   → MODIFY — redefine for comic formats
│   ├── Download/               #   KEEP — all download client integrations
│   ├── Indexers/               #   KEEP — Newznab/Torznab/all indexer implementations
│   ├── Notifications/          #   KEEP — all notification integrations
│   ├── DecisionEngine/         #   MODIFY — comic-specific decision specs
│   ├── ImportLists/            #   MODIFY — adapt for comic list sources
│   ├── Profiles/               #   MODIFY — metadata profile for comics
│   ├── Datastore/              #   MODIFY — migration for schema changes
│   └── [everything else]       #   KEEP — jobs, health, history, config, etc.
├── NzbDrone.Host/              # KEEP — ASP.NET hosting, startup
├── Readarr.Api.V1/             # RENAME → Panelarr.Api.V1/ — resource renaming
│   ├── Author/                 #   → Series/
│   ├── Books/                  #   → Issues/
│   ├── BookFiles/              #   → ComicFiles/
│   ├── Editions/               #   → REMOVE (Format entity eliminated, Issue owns ComicFiles directly)
│   ├── Series/                 #   → SeriesGroups/ (Readarr's "Series" = book series)
│   └── [rest]                  #   KEEP with resource renaming
├── Readarr.Http/               # RENAME → Panelarr.Http/ — minimal changes
├── NzbDrone.SignalR/           # KEEP
├── NzbDrone.Update/            # KEEP
├── NzbDrone.Mono/              # KEEP
├── NzbDrone.Windows/           # KEEP
└── [Test projects]             # MODIFY — update for new domain model
```

---

## 3. Domain Model Mapping

### 3.1 Entity Mapping (Readarr → Panelarr)

| Readarr Entity | Readarr Purpose | Panelarr Entity | Panelarr Purpose |
|---|---|---|---|
| `Author` | The author being tracked | **`Series`** | A comic series/run (= CV Volume) |
| `AuthorMetadata` | Author details from provider | **`SeriesMetadata`** | Series details from Metron |
| `Book` | A single book | **`Issue`** | A single comic issue |
| `Edition` | A format variant of a book | **Removed** | Not applicable — comics don't have editions like books. File format is a property on ComicFile. TPBs/HCs differentiated by IssueType. |
| `Series` | A book series grouping | **`SeriesGroup`** | Optional grouping of related Series |
| `SeriesBookLink` | Book ↔ Series join | **`SeriesGroupLink`** | Series ↔ SeriesGroup join |
| `BookFile` | Physical file on disk | **`ComicFile`** | Physical comic file on disk |

**Model chain:** `SeriesMetadata (← Publisher) → Series → Issue → ComicFile` (no Edition/Format layer. Publisher is referenced by SeriesMetadata via FK, not a parent in an ownership chain.)

**Note on Issue FK:** Issue links to `SeriesMetadataId` (not `SeriesId`), matching Readarr's pattern where Book links to AuthorMetadataId. To find the library Series for an Issue, join through SeriesMetadata. This is an inherited design — changing it would require refactoring throughout the codebase.

### 3.2 Entity Definitions

**SeriesMetadata** (was `AuthorMetadata`)
```csharp
// Fields to keep (renamed)
ForeignSeriesId     // was ForeignAuthorId — Metron series ID
TitleSlug           // URL-safe slug
Name                // Series title, e.g. "Batman"
SortName            // For alphabetical sorting
Overview            // Description
Status              // Ongoing | Complete | Cancelled | Hiatus (was Continuing | Ended)
Images              // Cover art
Links               // External links
Genres              // Genre tags

// Fields to remove
NameLastFirst, SortNameLastFirst  // Author-specific, not relevant
Gender, Hometown, Born, Died      // Author biographical data
Aliases                            // Author aliases

// Fields to add
Year                // Publication start year (for disambiguation)
PublisherId          // FK to Publisher entity
SeriesType           // enum: Single | TPB | GraphicNovel | Hardcover | Omnibus | OneShot | Annual | Limited
VolumeNumber         // Volume number within a SeriesGroup (nullable)
Ratings              // Rating data (kept from AuthorMetadata)
```

**Series** (was `Author`)
```csharp
// Fields to keep (renamed)
SeriesMetadataId    // was AuthorMetadataId
CleanName           // Normalized name for matching
Monitored           // Is this series being tracked?
MonitorNewItems     // Auto-monitor new issues?
Path                // Library folder path
RootFolderPath      // Root folder
Added               // Date added
QualityProfileId    // Quality profile FK
Tags                // User tags

// Fields to keep as-is
LastInfoSync, AddOptions

// Fields to remove
MetadataProfileId   // Replaced by simpler comic-specific filtering
```

**Issue** (was `Book`)
```csharp
// Fields to keep (renamed)
SeriesMetadataId    // was AuthorMetadataId
ForeignIssueId      // was ForeignBookId — Metron issue ID
TitleSlug
Title               // Issue title (often empty for comics)
ReleaseDate
CleanTitle
Monitored
LastInfoSync, LastSearchTime, Added, AddOptions
Links, Genres, Ratings

// Fields to remove
ForeignEditionId    // Edition entity removed — no equivalent in Panelarr
RelatedBooks        // Not applicable
AnyEditionOk        // Replaced by format preference

// Fields to add
IssueNumber          // float — supports .1, .5 numbering (e.g., #0.5)
IssueType            // enum: Standard | Annual | Special | OneShot | TPB | Hardcover | Omnibus
CoverArtUrl          // Primary cover image
PageCount            // From metadata provider
```

**~~Edition~~ (Removed)**

The Edition → Format mapping is eliminated. Comics don't have "editions" the way books do (hardcover, paperback, ebook). The file format (CBZ, CBR, PDF) is a property of the physical file, and TPB/HC/Omnibus distinctions are handled by `IssueType` on Issue. ComicFile links directly to Issue.

**ComicFile** (was `BookFile`)
```csharp
// Fields to keep
Path, Size, Modified, DateAdded
OriginalFilePath, SceneName, ReleaseGroup
Quality, IndexerFlags, MediaInfo
IssueId             // was EditionId — direct link to Issue (Format layer removed)
Part                // For multi-part archives (unlikely for comics, but keep)

// Fields to remove
CalibreId           // Calibre integration not relevant

// Fields to add
ComicFormat          // enum: CBZ | CBR | CB7 | PDF | EPUB (detected from file)
ImageCount           // Number of pages/images in archive
ImageQualityScore    // Calculated from image dimensions/compression
```

### 3.3 New Entities

**Publisher**
```csharp
public class Publisher : Entity<Publisher>
{
    public string Name { get; set; }
    public string CleanName { get; set; }
    public string ForeignPublisherId { get; set; }  // Metron publisher ID
    public string Description { get; set; }
    public List<MediaCover> Images { get; set; }     // Logo
}
```
- Separate table, referenced by SeriesMetadata.PublisherId
- Populated from metadata provider
- Used in folder naming token `{Publisher}`

### 3.4 Status Enum Changes

```csharp
// Readarr
public enum AuthorStatusType { Continuing = 0, Ended = 1 }

// Panelarr
public enum SeriesStatusType
{
    Continuing = 0,  // Ongoing
    Ended = 1,       // Complete
    Cancelled = 2,   // New
    Hiatus = 3       // New
}

// New enum
public enum SeriesType
{
    Single = 0,
    TPB = 1,
    GraphicNovel = 2,
    Hardcover = 3,
    Omnibus = 4,
    OneShot = 5,
    Annual = 6,
    Limited = 7
}

// SeriesType vs IssueType:
// - SeriesType lives on SeriesMetadata — describes what the series IS (from Metron)
//   e.g., a "Single" series primarily contains standard issues
// - IssueType lives on Issue — describes what each individual issue IS
//   e.g., a Single-type series can contain Annual or Special issues
// - A TPB-type series would have all its issues as IssueType.TPB
// - A Single-type series can also contain IssueType.TPB entries (collected editions within the same run)

// New enum — covers both issue variants AND collected edition types
public enum IssueType
{
    Standard = 0,
    Annual = 1,
    Special = 2,
    OneShot = 3,
    TPB = 4,          // Trade Paperback (collected edition)
    Hardcover = 5,     // Hardcover collected edition
    Omnibus = 6        // Omnibus collected edition
}

// New enum
public enum ComicFormat
{
    CBZ = 0,
    CBR = 1,
    CB7 = 2,
    PDF = 3,
    EPUB = 4,
    Unknown = 99
}
```

---

## 4. Metadata Provider Architecture

### 4.1 Readarr's Current Structure

Readarr uses two interfaces and two implementations:
```
IProvideAuthorInfo  → BookInfoProxy (primary, cloud API)
IProvideBookInfo    → BookInfoProxy
ISearchForNewBook   → BookInfoProxy (delegates search to GoodreadsSearchProxy)
ISearchForNewAuthor → BookInfoProxy

IProvideSeriesInfo  → GoodreadsProxy (legacy, direct Goodreads XML API)
```

The `BookInfoProxy` talks to a Readarr-hosted cloud metadata API that proxies/caches Goodreads data. Search goes through `GoodreadsSearchProxy` which calls Goodreads' JSON autocomplete endpoint.

### 4.2 Panelarr's Metadata Architecture

Replace both implementations with a clean `IMetadataProvider` abstraction:

```csharp
public interface IMetadataProvider
{
    // Series (was Author)
    Task<ProviderSeries> GetSeriesInfo(string foreignSeriesId);
    Task<List<ProviderSeries>> SearchSeries(string query, int? year = null, string publisher = null);
    Task<HashSet<string>> GetChangedSeries(DateTime since);

    // Issues (was Book)
    Task<List<ProviderIssue>> GetIssues(string foreignSeriesId);
    Task<ProviderIssue> GetIssueInfo(string foreignIssueId);

    // Publisher
    Task<ProviderPublisher> GetPublisher(string foreignPublisherId);

    // Discovery
    Task<List<ProviderIssue>> GetNewReleases(DateTime date);
}
```

**Provider DTOs** (separate from domain entities):
```csharp
public class ProviderSeries {
    public string ForeignId { get; set; }
    public string Title { get; set; }
    public int? Year { get; set; }
    public int? VolumeNumber { get; set; }
    public string Publisher { get; set; }
    public string PublisherForeignId { get; set; }
    public SeriesType Type { get; set; }
    public SeriesStatusType Status { get; set; }
    public string Description { get; set; }
    public string CoverUrl { get; set; }       // Mapped to Images list by MetronMapper
    public int IssueCount { get; set; }
    public List<string> Genres { get; set; }
    public List<string> Links { get; set; }
    public decimal? Rating { get; set; }
}

public class ProviderIssue {
    public string ForeignId { get; set; }
    public string SeriesForeignId { get; set; }
    public float IssueNumber { get; set; }
    public IssueType Type { get; set; }
    public string Title { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public string CoverUrl { get; set; }       // Mapped to CoverArtUrl / Images by MetronMapper
    public int? PageCount { get; set; }
    public string Description { get; set; }
    public List<string> Genres { get; set; }
    public List<string> Links { get; set; }
    public decimal? Rating { get; set; }
}

public class ProviderPublisher {
    public string ForeignId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string LogoUrl { get; set; }
}
```

### 4.3 MetronProvider Implementation

```
MetronProvider : IMetadataProvider
  ├── MetronApiClient (HTTP client, handles auth + rate limiting)
  ├── MetronMapper (ProviderSeries/ProviderIssue → domain entities)
  └── MetronCache (ICachedHttpResponseService wrapper)
```

**Metron API Endpoints Used:**
```
GET /api/series/?name={query}&year_began={year}     → SearchSeries
GET /api/series/{id}/                                → GetSeriesInfo
GET /api/issue/?series_id={id}                       → GetIssues
GET /api/issue/{id}/                                 → GetIssueInfo
GET /api/publisher/{id}/                             → GetPublisher
```

**Rate Limit Strategy:**
- Metron: 20 req/min, 5,000 req/day
- Implement token bucket rate limiter in `MetronApiClient`
- Aggressive local caching: series metadata cached 24h, issues cached 12h
- Bulk operations (adding a series with 100+ issues): fetch issue list in single paginated call, not per-issue
- Background refresh staggered across the day to stay under daily limit

### 4.4 Mapping Flow

```
User searches "Batman" in UI
  → API: GET /api/v1/series/lookup?term=batman
    → MetronProvider.SearchSeries("batman")
      → Metron API: GET /api/series/?name=batman
      → MetronMapper.MapSeries(response) → List<ProviderSeries>
    → Enrich with local DB IDs (existing series detection)
  → Return SeriesResource[] to UI

User adds a series
  → API: POST /api/v1/series
    → MetronProvider.GetSeriesInfo(foreignId)
    → MetronProvider.GetIssues(foreignId)
    → Map to domain: Series + SeriesMetadata + Issue[]
    → Persist to database
    → Trigger search if requested
```

---

## 5. Release Parser

### 5.1 Why Readarr's Parser Must Be Replaced

Readarr's parser is designed for books: `Author - Book Title (Year)` patterns with audio codec detection (MP3, FLAC, etc.). Comic releases follow fundamentally different naming conventions:

```
# Typical comic release names
Batman 2016 001 (2016) (Digital) (Zone-Empire).cbz
Amazing Spider-Man v5 015 (2022) (Digital) (Shan-Empire).cbr
Saga 066 (2024) (digital) (Son of Ultron-Empire).cbz
Batman - The Dark Knight Returns 01 (of 04) (1986).cbz
X-Men Annual 001 (2023).cbz
Batman TPB Vol 03 - Death of the Family (2013).cbz
```

Key differences:
- Issue numbers (not present in book releases)
- Volume indicators (`v5`, `Vol. 3`)
- Limited series markers (`01 of 04`, `01 (of 04)`)
- Group/scanner tags (`Zone-Empire`, `Shan-Empire`)
- Format already in extension (`.cbz`, `.cbr`)
- Year can appear multiple times (series year + issue year)
- No author in filename (series name instead)

### 5.2 Comic Parser Design

**ParsedComicInfo** (replaces `ParsedBookInfo`):
```csharp
public class ParsedComicInfo
{
    public string SeriesTitle { get; set; }
    public float? IssueNumber { get; set; }         // null for TPBs
    public int? VolumeNumber { get; set; }           // v5, Vol. 3
    public int? Year { get; set; }                   // Publication year
    public IssueType IssueType { get; set; }         // Standard, Annual, Special, TPB, etc.
    public int? TotalIssues { get; set; }            // "of 04" → 4
    public ComicFormat Format { get; set; }          // From extension
    public string ReleaseGroup { get; set; }         // Zone-Empire, etc.
    public QualityModel Quality { get; set; }
    public string ReleaseTitle { get; set; }         // Original title
    public string Source { get; set; }               // Digital, Print, Scan
}
```

**Key Regex Patterns Needed:**
```csharp
// Issue number extraction (most critical pattern)
// Matches: #001, 001, Issue 1, No. 1, No 1
IssueNumberRegex = @"(?:#|Issue\s*|No\.?\s*)(\d{1,4}(?:\.\d)?)"

// Volume detection
VolumeRegex = @"(?:v|vol\.?\s*)(\d{1,2})"

// Year extraction
YearRegex = @"\((\d{4})\)"

// Limited series
LimitedSeriesRegex = @"(\d{1,3})\s*(?:of|\/)\s*(\d{1,3})"

// Annual/Special detection
AnnualRegex = @"\bannual\b"
SpecialRegex = @"\bspecial\b"

// TPB detection
TPBRegex = @"\b(?:TPB|Trade\s*Paperback|HC|Hardcover|Omnibus)\b"

// Source detection
SourceRegex = @"\b(?:Digital|Print|Scan|c2c|noads)\b"

// Release group (typically last parenthetical or after last hyphen)
ReleaseGroupRegex = @"\(([^)]+(?:Empire|Minutemen|DCP|Mephisto|Digi))\)"
```

### 5.3 Parsing Pipeline

```
Input: "Amazing Spider-Man v5 015 (2022) (Digital) (Shan-Empire).cbz"

1. Extract extension → ComicFormat.CBZ
2. Remove extension
3. Extract release group → "Shan-Empire"
4. Extract source → "Digital"
5. Extract year(s) → [2022]
6. Extract issue number → 15
7. Extract volume → 5
8. Check for annual/special/TPB markers → Standard
9. Remaining text = series title → "Amazing Spider-Man"
10. Clean and normalize title
11. Build ParsedComicInfo
```

---

## 6. File Organizer

### 6.1 Readarr's Token System (Reusable)

Readarr's `FileNameBuilder` uses a regex-based token replacement system:
```
{prefix token:customFormat suffix}
```

The template engine, separator handling, case conversion, and cleanup logic are **fully reusable**. We only need to change the available tokens and their handlers.

### 6.2 Panelarr Naming Tokens

**Series Tokens** (replace Author tokens):
| Token | Example Output | Source |
|---|---|---|
| `{Series Title}` | `Batman` | SeriesMetadata.Name |
| `{Series CleanTitle}` | `Batman` | Series.CleanName |
| `{Series TitleThe}` | `Batman, The` | Title with "The" moved |
| `{Series Year}` | `2016` | SeriesMetadata.Year |
| `{Series Type}` | `Single` | SeriesMetadata.SeriesType |
| `{Publisher}` | `DC Comics` | Publisher.Name |

**Issue Tokens** (replace Book tokens):
| Token | Example Output | Source |
|---|---|---|
| `{Issue Number}` | `1` | Issue.IssueNumber |
| `{Issue Number:000}` | `001` | Zero-padded (custom format) |
| `{Issue Title}` | `I Am Gotham Part 1` | Issue.Title |
| `{Issue Type}` | `Annual` | Issue.IssueType (also usable in folder format for subfolder separation) |
| `{Release Year}` | `2016` | Issue.ReleaseDate.Year |

**File/Quality Tokens** (keep from Readarr):
| Token | Example Output |
|---|---|
| `{Original Title}` | Scene release name |
| `{Original Filename}` | Original filename |
| `{Release Group}` | `Zone-Empire` |
| `{Quality Full}` | `CBZ` |
| `{Custom Formats}` | Applied custom formats |

### 6.3 Default Naming Config

```csharp
// Standard Issue Format
"{Series Title} ({Series Year}) #{Issue Number:000}"
// → Batman (2016) #001.cbz

// Annual Format
"{Series Title} ({Series Year}) Annual #{Issue Number:000}"
// → Batman (2016) Annual #001.cbz

// TPB Format
"{Series Title} ({Series Year}) Vol {Issue Number:00} TPB"
// → Batman (2016) Vol 01 TPB.cbz

// Series Folder Format
"{Publisher}/{Series Title} ({Series Year})"
// → DC Comics/Batman (2016)

// Optional: subfolder by issue type
// "{Publisher}/{Series Title} ({Series Year})/{Issue Type}"
// → DC Comics/Batman (2016)/Standard/Batman (2016) #001.cbz
// → DC Comics/Batman (2016)/TPB/Batman (2016) Vol 01 TPB.cbz
```

### 6.4 NamingConfig Schema Change

```csharp
public class NamingConfig : ModelBase
{
    public bool RenameComics { get; set; }            // was RenameBooks
    public bool ReplaceIllegalCharacters { get; set; }
    public ColonReplacementFormat ColonReplacementFormat { get; set; }
    public string StandardIssueFormat { get; set; }   // was StandardBookFormat
    public string AnnualIssueFormat { get; set; }     // NEW
    public string TPBFormat { get; set; }             // NEW — collected editions
    public string SeriesFolderFormat { get; set; }    // was AuthorFolderFormat
}
```

**`{Issue Type}` folder token:** Available in `SeriesFolderFormat` for users who want subfolder separation. Resolves to "Standard", "Annual", "TPB", "Hardcover", "Omnibus" based on the issue being processed.

---

## 7. Quality Definitions

### 7.1 Readarr Qualities (Replace)

Readarr defines qualities for ebook/audiobook formats (EPUB, MOBI, AZW3, PDF, MP3, FLAC, etc.). These must be replaced with comic-specific qualities.

### 7.2 Panelarr Quality Definitions

```csharp
public enum Quality
{
    Unknown = 0,
    PDF = 1,
    CBR = 2,        // RAR archive (legacy, less preferred)
    CBZ = 3,         // ZIP archive (preferred)
    CB7 = 4,         // 7z archive
    EPUB = 5,        // Fixed-layout EPUB
    CBZ_HD = 10,     // CBZ with high-res images (>2000px width)
    CBZ_Web = 11,    // CBZ web quality (<1500px width)
}
```

**Default Quality Profile:**
```
Cutoff: CBZ
Allowed: CBZ_HD > CBZ > CB7 > CBR > PDF > EPUB > CBZ_Web
```

### 7.3 Custom Format Specs

Comic-specific custom format conditions:
- **File extension**: `.cbz`, `.cbr`, `.cb7`, `.pdf`, `.epub`
- **Image resolution**: Based on CBZ/CBR internal image dimensions
- **Source**: Digital vs Scan vs c2c (cover-to-cover)
- **Release group**: Preferred scanner groups
- **Page count**: Minimum pages (detect incomplete rips)
- **Tag matching**: "noads", "digital", "c2c"

---

## 8. Database Strategy

### 8.1 Migration Approach

Readarr has 40 migrations (000-039). Rather than replaying Readarr's migration history, Panelarr will:

1. **Start with a single initial migration** that creates the Panelarr schema from scratch
2. This migration defines all tables with Panelarr naming and comic-specific columns
3. Readarr's migration history is preserved in source control for reference but not executed

This is cleaner than trying to migrate from Readarr's schema, since:
- We're renaming most tables and many columns
- We're adding new tables (Publisher) and removing others
- No user will be "upgrading" from Readarr to Panelarr — it's a new install

### 8.2 Schema (Initial Migration)

```sql
-- Core entities
CREATE TABLE Publishers (
    Id              INTEGER PRIMARY KEY,
    Name            TEXT NOT NULL,
    CleanName       TEXT NOT NULL,
    ForeignPublisherId TEXT UNIQUE,
    Description     TEXT,
    Images          TEXT  -- JSON
);

CREATE TABLE SeriesMetadata (
    Id              INTEGER PRIMARY KEY,
    ForeignSeriesId TEXT NOT NULL UNIQUE,
    TitleSlug       TEXT NOT NULL UNIQUE,
    Name            TEXT NOT NULL,
    SortName        TEXT NOT NULL,
    Year            INTEGER,
    Overview        TEXT,
    Status          INTEGER NOT NULL DEFAULT 0,  -- SeriesStatusType
    SeriesType      INTEGER NOT NULL DEFAULT 0,  -- SeriesType enum
    VolumeNumber    INTEGER,
    PublisherId     INTEGER REFERENCES Publishers(Id),
    Images          TEXT,   -- JSON
    Links           TEXT,   -- JSON
    Genres          TEXT,   -- JSON
    Ratings         TEXT    -- JSON
);

CREATE TABLE Series (
    Id                  INTEGER PRIMARY KEY,
    SeriesMetadataId    INTEGER NOT NULL UNIQUE REFERENCES SeriesMetadata(Id),
    CleanName           TEXT NOT NULL,
    Path                TEXT,
    RootFolderPath      TEXT,
    Monitored           INTEGER NOT NULL DEFAULT 1,
    MonitorNewItems     INTEGER NOT NULL DEFAULT 0,
    LastInfoSync        TEXT,  -- DateTime
    QualityProfileId    INTEGER NOT NULL,
    Tags                TEXT,  -- JSON
    Added               TEXT NOT NULL,  -- DateTime
    AddOptions          TEXT   -- JSON
);
CREATE INDEX IX_Series_CleanName ON Series(CleanName);
CREATE INDEX IX_Series_Path ON Series(Path);

CREATE TABLE Issues (
    Id                  INTEGER PRIMARY KEY,
    SeriesMetadataId    INTEGER NOT NULL REFERENCES SeriesMetadata(Id),
    ForeignIssueId      TEXT NOT NULL UNIQUE,
    TitleSlug           TEXT NOT NULL UNIQUE,
    Title               TEXT,
    IssueNumber         REAL NOT NULL,  -- float for .5 issues
    IssueType           INTEGER NOT NULL DEFAULT 0,
    ReleaseDate         TEXT,  -- DateTime
    CoverArtUrl         TEXT,
    PageCount           INTEGER,
    CleanTitle          TEXT,
    Monitored           INTEGER NOT NULL DEFAULT 1,
    LastInfoSync        TEXT,
    LastSearchTime      TEXT,
    Added               TEXT NOT NULL,
    AddOptions          TEXT,  -- JSON
    Links               TEXT,  -- JSON
    Genres              TEXT,  -- JSON
    Ratings             TEXT   -- JSON
);
-- ForeignIssueId index omitted: UNIQUE constraint creates implicit index
CREATE INDEX IX_Issues_CleanTitle ON Issues(CleanTitle);
CREATE INDEX IX_Issues_SeriesMetadataId_ReleaseDate ON Issues(SeriesMetadataId, ReleaseDate);

-- Format/Edition table REMOVED — Issue owns ComicFiles directly

CREATE TABLE SeriesGroups (
    Id                  INTEGER PRIMARY KEY,
    ForeignSeriesGroupId TEXT UNIQUE,
    Title               TEXT NOT NULL,
    Description         TEXT,
    SortTitle           TEXT
);

CREATE TABLE SeriesGroupLinks (
    Id                  INTEGER PRIMARY KEY,
    SeriesGroupId       INTEGER NOT NULL REFERENCES SeriesGroups(Id) ON DELETE CASCADE,
    SeriesMetadataId    INTEGER NOT NULL REFERENCES SeriesMetadata(Id) ON DELETE CASCADE,
    Position            TEXT,              -- free-text position label (e.g., "3.5", inherited from Readarr)
    SeriesPosition      INTEGER NOT NULL DEFAULT 0,  -- integer sort order within the group
    IsPrimary           INTEGER NOT NULL DEFAULT 1
);
CREATE INDEX IX_SeriesGroupLinks_SeriesGroupId ON SeriesGroupLinks(SeriesGroupId);
CREATE INDEX IX_SeriesGroupLinks_SeriesMetadataId ON SeriesGroupLinks(SeriesMetadataId);

CREATE TABLE ComicFiles (
    Id                  INTEGER PRIMARY KEY,
    IssueId             INTEGER NOT NULL REFERENCES Issues(Id),
    Path                TEXT NOT NULL UNIQUE,
    Size                INTEGER NOT NULL,
    Modified            TEXT NOT NULL,
    DateAdded           TEXT NOT NULL,
    OriginalFilePath    TEXT,
    SceneName           TEXT,
    ReleaseGroup        TEXT,
    Quality             TEXT NOT NULL,  -- JSON
    IndexerFlags        INTEGER NOT NULL DEFAULT 0,
    MediaInfo           TEXT,    -- JSON
    Part                INTEGER NOT NULL DEFAULT 1,
    ComicFormat         INTEGER NOT NULL DEFAULT 99, -- ComicFormat enum (99 = Unknown)
    ImageCount          INTEGER,
    ImageQualityScore   REAL
);
CREATE INDEX IX_ComicFiles_IssueId ON ComicFiles(IssueId);

-- Inherited tables (keep as-is from Readarr, no renaming needed)
-- Config, RootFolders, QualityProfiles, QualityDefinitions, CustomFormats,
-- DelayProfiles, NamingConfig (schema modified per Section 6.4), Notifications, ScheduledTasks, Commands,
-- Indexers, IndexerStatus, DownloadClients, DownloadClientStatus,
-- ImportLists, ImportListStatus, ImportListExclusions,
-- Tags, History, Blocklist, RemotePathMappings, Users,
-- MetadataFiles, ExtraFiles, DownloadHistory, CustomFilters
```

---

## 9. API Layer

### 9.1 Controller Mapping

| Readarr Controller | Endpoint | Panelarr Controller | Endpoint |
|---|---|---|---|
| `AuthorController` | `/api/v1/author` | **`SeriesController`** | `/api/v1/series` |
| `BookController` | `/api/v1/book` | **`IssueController`** | `/api/v1/issue` |
| `BookFileController` | `/api/v1/bookfile` | **`ComicFileController`** | `/api/v1/comicfile` |
| `EditionController` | `/api/v1/edition` | **Removed** | Format entity eliminated — file info accessed via ComicFile |
| `SeriesController` | `/api/v1/series` | **`SeriesGroupController`** | `/api/v1/seriesgroup` |
| `BookShelfController` | `/api/v1/bookshelf` | **`MonitorController`** | `/api/v1/monitor` |
| — | — | **`PublisherController`** (new) | `/api/v1/publisher` |
| All others | — | **Keep as-is** | Same endpoints |

### 9.2 Resource Classes

Each controller's Resource class follows the same rename pattern. The `RestController<TResource>` base class and `ProviderControllerBase` pattern are kept as-is — only the generic type parameters change.

### 9.3 SignalR Hubs

Readarr uses `RestControllerWithSignalR<TResource, TModel>` for real-time updates. The pattern is kept; only the resource/model types change.

---

## 10. Frontend

### 10.1 Current Stack

- React 16+ with Redux
- Redux connector pattern (container + presentational components)
- jQuery AJAX for API calls
- ~1,021 JS files

### 10.2 Modification Strategy

**Phase 1 — Minimum viable UI changes:**

1. **Rename all occurrences**: Author → Series, Book → Issue, BookFile → ComicFile (Edition/Format removed entirely)
2. **Rebrand**: Readarr → Panelarr (logos, titles, colors)
3. **Adapt views**:
   - Author Index → Series Index (grid of comic series with cover art)
   - Author Detail → Series Detail (list of issues with status)
   - Book Detail → Issue Detail (issue metadata, file info)
4. **Add new fields to forms**: IssueNumber, SeriesType, Publisher
5. **Remove irrelevant UI**: Calibre integration, audiobook-specific options

**Phase 1 does NOT require:**
- Framework migration (React 16 → 18)
- State management rewrite (Redux stays)
- New views (Calendar, SeriesGroup views are Phase 2)

### 10.3 Key Component Mapping

```
frontend/src/
├── Author/          → Series/
│   ├── Index/       → SeriesIndex (grid + table views)
│   └── Details/     → SeriesDetails (issue list)
├── Book/            → Issue/
│   └── Details/     → IssueDetails
├── Activity/        → KEEP (History, Queue, Blocklist)
├── Calendar/        → KEEP (release calendar)
├── Settings/        → KEEP (rename labels only)
├── Wanted/          → KEEP (missing issues view)
├── Components/      → KEEP (shared UI)
└── Store/
    ├── Actions/
    │   ├── authorActions.js  → seriesActions.js
    │   ├── bookActions.js    → issueActions.js
    │   └── [rest]            → KEEP
    └── Reducers/             → Mirror action renames
```

---

## 11. Component Dependency Map

What touches what — to guide the order of implementation:

```
                    ┌─────────────────┐
                    │  MetronProvider  │ ← REPLACE (new)
                    │  (IMetadata      │
                    │   Provider)      │
                    └────────┬────────┘
                             │ provides data to
                    ┌────────▼────────┐
                    │  Domain Model   │ ← MODIFY
                    │  Series, Issue, │
                    │  ComicFile      │
                    └───┬────┬────┬───┘
               ┌────────┘    │    └────────┐
               ▼             ▼             ▼
        ┌──────────┐  ┌──────────┐  ┌──────────┐
        │  Parser  │  │ Organizer│  │ Decision │  ← MODIFY/REPLACE
        │ (comic   │  │ (naming  │  │ Engine   │
        │ releases)│  │ tokens)  │  │ (specs)  │
        └────┬─────┘  └────┬─────┘  └────┬─────┘
             │              │              │
             ▼              ▼              ▼
        ┌──────────────────────────────────────┐
        │         Inherited Infrastructure      │  ← KEEP
        │  Indexers, Download Clients, Jobs,    │
        │  Notifications, Auth, SignalR, etc.   │
        └──────────────────────────────────────┘
             │              │              │
             ▼              ▼              ▼
        ┌──────────┐  ┌──────────┐  ┌──────────┐
        │   API    │  │ Frontend │  │ Database │  ← RENAME + ADAPT
        │ (REST)   │  │ (React)  │  │ (SQLite/ │
        │          │  │          │  │ Postgres)│
        └──────────┘  └──────────┘  └──────────┘
```

---

## 12. Implementation Order

Based on the dependency map, the recommended implementation order for Phase 1:

### Step 1: Foundation (no runtime dependencies)
1. Fork Readarr repo, rename solution/projects
2. Global find-and-replace: Readarr → Panelarr branding
3. Create initial database migration (Section 8.2)
4. Define new enums (SeriesStatusType, SeriesType, IssueType, ComicFormat)

### Step 2: Domain Model (everything else depends on this)
5. Rename entity classes: Author → Series, Book → Issue, etc.
6. Update all properties per Section 3.2
7. Add Publisher entity
8. Update all repository/service classes for new entity names
9. Update LazyLoaded relationships

### Step 3: Metadata Provider (populates the domain)
10. Define `IMetadataProvider` interface
11. Implement `MetronProvider` with rate limiting and caching
12. Implement mapping: Metron DTOs → domain entities
13. Wire into DI container

### Step 4: Parser (enables search and import)
14. Implement `ComicParser` with comic-specific regex patterns
15. Implement `ParsedComicInfo` model
16. Update `ParsingService` for comic matching logic

### Step 5: Organizer (enables file management)
17. Update `FileNameBuilder` with comic tokens (Section 6.2)
18. Update `NamingConfig` schema with per-IssueType templates (Standard, Annual, TPB)
19. Set default naming templates
20. Add `{Issue Type}` folder token

### Step 5.5: ComicInfo.xml Embedding (enables reader integration)
21. Implement ComicInfo.xml generation from Issue + Series metadata
22. Embed ComicInfo.xml inside CBZ files on import/rename
23. Include: series title, issue number, year, publisher, page count, issue type, cover art reference

### Step 6: Quality System
24. Define comic quality definitions (CBZ, CBR, PDF, etc.)
25. Update default quality profile
26. Add comic-specific custom format conditions

### Step 7: API + Frontend
27. Rename API controllers and resources
28. Update frontend components (rename + add comic fields)
29. Update API endpoint URLs

### Step 8: Integration Testing
30. Verify indexer search → parse → grab → download → import pipeline
31. Verify metadata provider → series add → issue monitoring flow
32. Docker image build and test

---

## 13. Open Questions — Resolved

| # | Question from PRD | Resolution |
|---|---|---|
| 1 | Release parsing complexity | **Full rewrite needed.** Readarr's parser uses book-specific patterns (Author - Title). Comic naming is fundamentally different. Keep the `ParsingService` orchestration, replace all regex patterns and `ParsedBookInfo` model. |
| 2 | Metron rate limits | **Token bucket + aggressive caching.** 20 req/min handled with in-process rate limiter. Series cached 24h, issues 12h. Bulk issue fetch via paginated list call. Daily limit (5K) manageable for typical usage. |
| 3 | CBZ inspection library | **SharpCompress** for archive reading (already a NzbDrone dependency). Supports ZIP (CBZ), RAR (CBR), 7z (CB7). RAR5 is supported. Image dimension extraction via System.Drawing or ImageSharp. |
| 4 | Database migration path | **Clean initial migration.** New schema, not a migration from Readarr. No user upgrades from Readarr to Panelarr. See Section 8.1. |
| 5 | UI rebranding scope | **Rename + minimal adaptation in Phase 1.** Change labels, add IssueNumber/Publisher fields, remove Calibre/audiobook UI. No framework migration. See Section 10.2. |

---

## 14. Technology Stack Summary

| Component | Technology | Source |
|---|---|---|
| Runtime | .NET 8+ | Inherited |
| Language | C# 12 | Inherited |
| Web Framework | ASP.NET Core | Inherited |
| Database | SQLite (default) + PostgreSQL | Inherited |
| ORM | Dapper + FluentMigrator | Inherited |
| Frontend | React 16 + Redux | Inherited |
| Real-time | SignalR | Inherited |
| HTTP Client | NzbDrone.Common.Http | Inherited |
| Caching | LazyCache + CacheManager | Inherited |
| Validation | FluentValidation | Inherited |
| Archive Reading | SharpCompress | Inherited (verify) |
| Metadata API | Metron REST API | New |
| Comic Parsing | Custom regex parser | New |
| Containerization | Docker (multi-arch) | Inherited |

---

## 15. Risk Register

| Risk | Impact | Likelihood | Mitigation |
|---|---|---|---|
| Metron API goes down or changes | No metadata for new series | Low | Local cache, ComicVine fallback (Phase 2), monitor uptime |
| Metron rate limits too restrictive for power users | Slow series additions | Medium | Aggressive caching, batch requests, backoff strategy |
| Comic release name parsing misses edge cases | Failed imports, wrong matches | High | Start with common patterns, build regex test suite from real releases, iterate |
| Readarr upstream diverges significantly | Merge conflicts when pulling fixes | Medium | Track upstream changes, cherry-pick selectively, don't diverge unnecessarily in infrastructure |
| SharpCompress can't handle corrupt CBR/CBZ | Import failures | Low | Graceful fallback: import file without inspection, log warning |
| Frontend rename introduces subtle bugs | Broken UI | Medium | Systematic find-replace with grep verification, manual testing |

---

## 16. Future Features

### 16.1 Story Arcs (Post-Phase 3)

Story arcs span multiple Series — e.g., "Civil War" pulls issues from 30+ Marvel titles. Panelarr can use this for acquisition ("monitor and download all Civil War issues") and pass reading order to readers via ComicInfo.xml.

**Data Model:**
```csharp
public class StoryArc : Entity<StoryArc>
{
    public string ForeignStoryArcId { get; set; }  // Metron arc ID
    public string Title { get; set; }               // "Civil War"
    public string Description { get; set; }
}

public class StoryArcIssueLink : Entity<StoryArcIssueLink>
{
    public int StoryArcId { get; set; }
    public int IssueId { get; set; }
    public int ReadingOrder { get; set; }            // Position in the arc
}
```

**Key decisions:**
- Files stay in their Series folders — story arc is a virtual grouping, not a folder structure
- Story arc metadata embedded in ComicInfo.xml so Kavita/Komga can build reading lists
- Metron has story arc data with reading order positions
- "Add story arc" flow monitors specific issues across multiple Series
- Depends on having a solid multi-series library first (Phase 1-2 complete)
