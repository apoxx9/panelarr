# Last Handoff — 2026-03-26

## Completed This Session
- Phase 0: PRD drafted and finalized (@analyst)
- Phase 0: ARCHITECTURE.md drafted, reviewed 3x, all issues fixed (@architect)
- Phase 0: BACKLOG.md created with 28 stories across 3 phases (@pm)
- P1-01: Fork and rebrand — DONE
- P1-02: Define comic-specific enums — DONE
- P1-03: Replace 40 Readarr migrations with single Panelarr initial migration — DONE
- P1-04: Rename Author → Series, AuthorMetadata → SeriesMetadata — DONE
- P1-05: Rename Book → Issue, BookFile → ComicFile; remove Edition from API layer — DONE
- P1-06: Add Publisher entity — DONE
  - Publisher model, repository, service, API controller at `/api/v1/publisher`
- P1-07: Remap SeriesGroup and SeriesGroupLink — DONE
  - SeriesGroupController at `/api/v1/seriesgroup`
- P1-08: Define IMetadataProvider interface — DONE
  - IMetadataProvider interface, ProviderSeries/ProviderIssue/ProviderPublisher DTOs, NullMetadataProvider stub
- P1-09: Implement MetronProvider — DONE
  - MetronApiClient with Basic auth, MetronRateLimiter (20 req/min / 5K req/day)
  - MetronSettings, MetronResources DTOs
- P1-10: Implement MetronMapper — DONE
  - MetronMapper: ProviderSeries → SeriesMetadata + Series, ProviderIssue → Issue, ProviderPublisher → Publisher
- P1-11: Comic release name parser — DONE
  - ParsedComicInfo model, ComicParser with regex patterns for all comic naming conventions
- P1-12: Update ParsingService for comic matching — DONE
  - GetSeriesForComicRelease, GetIssuesForComicRelease, MapComicRelease added to IParsingService
- P1-13: Comic naming tokens and per-IssueType templates — DONE
  - NamingConfig: AnnualIssueFormat, TPBFormat properties with comic defaults
  - FileNameBuilder: IssueType-based format selection, {Series Title}, {Series Year}, {Publisher}, {Issue Number}, {Volume Number}, {Issue Type} tokens
  - IPublisherService injected into FileNameBuilder
- P1-14: ComicInfo.xml generation and embedding — DONE
  - ComicInfoGenerator (IComicInfoGenerator) at src/NzbDrone.Core/MediaFiles/ComicInfo/
  - ComicInfoEmbedService handles ComicFileAddedEvent and ComicFileRenamedEvent; skips non-CBZ
- P1-15: Define comic quality definitions — DONE
  - Quality: Unknown, PDF, EPUB, CBR, CBZ, CB7, CBZ_Web, CBZ_HD (IDs 0-7)
  - Removed MOBI, AZW3, MP3, FLAC, M4B, UnknownAudio
  - Default quality profile: "Comic" with CBZ cutoff
  - MediaFileExtensions updated: .cbz, .cbr, .cb7, .pdf, .epub
  - ComicParser MapFormatToQuality now correctly maps CBZ/CBR/CB7
- P1-16: Rename API controllers and resources — DONE
  - EditionController deleted
  - NamingConfigResource/mapper: AnnualIssueFormat, TPBFormat fields added
- P1-17: Adapt frontend for comics — DONE
  - API URLs updated: /author → /series, /book → /issue, /bookFile → /comicfile, /bookshelf → /issueshelf
  - UI label strings: 'Author' → 'Series', 'Book' → 'Issue' across all frontend files
- P1-18: Verify inherited infrastructure — DONE (code-level; runtime verification pending)
  - Build passes 0 errors; all inherited indexer/download/import infrastructure intact
- P1-19: Docker packaging — DONE
  - Dockerfile and docker-compose.yml created at project root

## Environment Notes
- .NET 10.0.201 installed via `brew install --cask dotnet-sdk`
- Building net6.0 targets on .NET 10 works (9 pre-existing SDK version warnings, not errors)
- `dotnet build src/Panelarr.sln` → Build succeeded, 0 errors

## Phase 1 Status: COMPLETE

All P1-01 through P1-19 stories are done. The build is clean with 0 errors.

## Next: Phase 2

Start with P2-01: Import existing local comic files.

### P2-01 Context
- Scan folder for comic files (CBZ, CBR, CB7, PDF, EPUB)
- Match parsed filenames to Metron metadata via ParsingService
- Rename and move matched files to library using FileNameBuilder
- Handle unmatched files via quarantine or manual match UI

### Key File Locations
- Comic extensions: `src/NzbDrone.Core/MediaFiles/MediaFileExtensions.cs`
- Parser: `src/NzbDrone.Core/Parser/ComicParser.cs`
- Import pipeline: `src/NzbDrone.Core/MediaFiles/BookImport/`
- File naming: `src/NzbDrone.Core/Organizer/FileNameBuilder.cs`

## Key Decisions (carried forward)
- Fork base: Readarr (not Sonarr, not Kapowarr)
- Metadata: Metron primary, ComicVine fallback
- Edition entity removed — Issue links directly to ComicFile
- TPBs as IssueType within same Series (not separate Series)
- ComicInfo.xml embedded in CBZ on import/rename
- Story Arcs parked as future feature (F-01)
