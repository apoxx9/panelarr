using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles.LibraryImport
{
    public interface ILibraryImportProposalService
    {
        List<LibraryImportProposal> GetProposals(int rootFolderId);
    }

    // Groups a root folder's unmapped files into proposed series for the
    // library-import review screen. One proposal per folder (Mylar keeps one
    // series per folder); files that don't belong to the proposed series are
    // simply not matched at import time and stay unmapped.
    //
    // Identification precedence per folder (docs/tagged-library-import.md):
    //   1. cvinfo file        -> ComicVine volume id      (exact, no API call)
    //   2. files' tagged ids  -> issue id -> series       (exact, 1 API call)
    //   3. tags/folder name   -> provider name+year search (probable)
    public class LibraryImportProposalService : ILibraryImportProposalService
    {
        private const int TagSampleSize = 3;

        private static readonly Regex CvInfoVolumeRegex = new (@"comicvine\.gamespot\.com/.*?/4050-(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IMediaFileService _mediaFileService;
        private readonly IRootFolderService _rootFolderService;
        private readonly ISeriesService _seriesService;
        private readonly IMetadataTagService _metadataTagService;
        private readonly IProvideIssueInfo _issueInfo;
        private readonly ISearchForNewSeries _searchService;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public LibraryImportProposalService(IMediaFileService mediaFileService,
                                            IRootFolderService rootFolderService,
                                            ISeriesService seriesService,
                                            IMetadataTagService metadataTagService,
                                            IProvideIssueInfo issueInfo,
                                            ISearchForNewSeries searchService,
                                            IDiskProvider diskProvider,
                                            Logger logger)
        {
            _mediaFileService = mediaFileService;
            _rootFolderService = rootFolderService;
            _seriesService = seriesService;
            _metadataTagService = metadataTagService;
            _issueInfo = issueInfo;
            _searchService = searchService;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public List<LibraryImportProposal> GetProposals(int rootFolderId)
        {
            var rootFolder = _rootFolderService.Get(rootFolderId);

            var unmappedByFolder = _mediaFileService.GetUnmappedFiles()
                .Where(f => f.Path.IsNotNullOrWhiteSpace() && f.Path.StartsWith(rootFolder.Path, StringComparison.OrdinalIgnoreCase))
                .GroupBy(f => Path.GetDirectoryName(f.Path))
                .Where(g => g.Key.IsNotNullOrWhiteSpace());

            var proposals = new List<LibraryImportProposal>();

            foreach (var folderGroup in unmappedByFolder)
            {
                try
                {
                    var proposal = BuildProposal(folderGroup.Key, folderGroup.Select(f => f.Path).ToList());

                    if (proposal != null)
                    {
                        proposals.Add(proposal);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Failed to build import proposal for folder {0}", folderGroup.Key);
                }
            }

            return proposals.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private LibraryImportProposal BuildProposal(string folder, List<string> filePaths)
        {
            var tags = ReadSampleTags(filePaths);
            var parsedFolder = ComicParser.ParseRelease(Path.GetFileName(folder));

            var displayName = tags?.SeriesTitle;

            if (displayName.IsNullOrWhiteSpace())
            {
                displayName = parsedFolder?.SeriesTitle;
            }

            // The folder year is the series start year (taggers name folders
            // "Series (Year)"); a sampled file's tag year is just that issue's
            // cover year and reads wrong on the review screen ("Saga (2015)")
            var displayYear = parsedFolder?.Year ?? GuessYear(tags);

            // 1. cvinfo: the volume id, straight from the folder
            var cvInfoId = ReadCvInfoVolumeId(folder);

            if (cvInfoId != null)
            {
                return Build(folder, filePaths.Count, cvInfoId, displayName ?? Path.GetFileName(folder), displayYear, ProposalConfidence.Exact, "cvinfo");
            }

            // 2. tagged issue ids: resolve one representative to its series
            var taggedIssueId = ReadMajorityTaggedId(filePaths);

            if (taggedIssueId != null)
            {
                try
                {
                    var issueInfo = _issueInfo.GetIssueInfo(taggedIssueId);
                    var seriesForeignId = issueInfo?.Item1;

                    // The proxy echoes the issue id back when it cannot resolve
                    // the parent series — that is not a series id and must not
                    // be proposed
                    if (seriesForeignId.IsNotNullOrWhiteSpace() && seriesForeignId != taggedIssueId)
                    {
                        var metadata = issueInfo.Item3?.FirstOrDefault();
                        var name = metadata?.Name;

                        if (name.IsNullOrWhiteSpace())
                        {
                            name = displayName;
                        }

                        return Build(folder, filePaths.Count, seriesForeignId, name ?? Path.GetFileName(folder), metadata?.Year ?? displayYear, ProposalConfidence.Exact, "file tags");
                    }

                    _logger.Debug("Tagged id {0} did not resolve to a series for {1}; falling back to name search", taggedIssueId, folder);
                }
                catch (Exception ex)
                {
                    // Stale id (merged/deleted volume), rate limit, etc.
                    _logger.Debug(ex, "Tagged id {0} did not resolve for {1}; falling back to name search", taggedIssueId, folder);
                }
            }

            // 3. name + year search
            if (displayName.IsNullOrWhiteSpace())
            {
                _logger.Debug("No usable metadata for folder {0}; leaving its files unmapped", folder);
                return null;
            }

            var query = displayYear.HasValue ? $"{displayName} {displayYear}" : displayName;
            var hit = _searchService.SearchForNewSeries(query).FirstOrDefault();

            if (hit?.Metadata?.Value?.ForeignSeriesId == null)
            {
                _logger.Debug("Name search found nothing for folder {0} ('{1}')", folder, query);
                return null;
            }

            var source = taggedIssueId != null ? "name search (stale id)" : "name search";

            return Build(folder, filePaths.Count, hit.Metadata.Value.ForeignSeriesId, hit.Metadata.Value.Name, hit.Metadata.Value.Year, ProposalConfidence.Probable, source);
        }

        private LibraryImportProposal Build(string folder, int fileCount, string foreignId, string name, int? year, ProposalConfidence confidence, string idSource)
        {
            return new LibraryImportProposal
            {
                Folder = folder,
                ForeignSeriesId = foreignId,
                Name = name,
                Year = year,
                Confidence = confidence,
                IdSource = idSource,
                FileCount = fileCount,
                ExistingSeriesId = _seriesService.FindById(foreignId)?.Id
            };
        }

        private string ReadCvInfoVolumeId(string folder)
        {
            var cvInfoPath = Path.Combine(folder, "cvinfo");

            if (!_diskProvider.FileExists(cvInfoPath))
            {
                return null;
            }

            var match = CvInfoVolumeRegex.Match(_diskProvider.ReadAllText(cvInfoPath));

            return match.Success ? "cv:" + match.Groups[1].Value : null;
        }

        private Parser.Model.ParsedFileTagInfo ReadSampleTags(List<string> filePaths)
        {
            foreach (var path in filePaths.Take(TagSampleSize))
            {
                var tags = _metadataTagService.ReadTags(_diskProvider.GetFileInfo(path));

                if (tags.SeriesTitle.IsNotNullOrWhiteSpace())
                {
                    return tags;
                }
            }

            return null;
        }

        private string ReadMajorityTaggedId(List<string> filePaths)
        {
            return filePaths.Take(TagSampleSize)
                .Select(p => _metadataTagService.ReadTags(_diskProvider.GetFileInfo(p)).ForeignIssueId)
                .Where(id => id.IsNotNullOrWhiteSpace())
                .GroupBy(id => id)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;
        }

        // Display hint only — the provider's year wins once the series is added.
        private static int? GuessYear(Parser.Model.ParsedFileTagInfo tags)
        {
            if (tags == null)
            {
                return null;
            }

            return tags.Year >= 1900 && tags.Year <= 2100 ? (int)tags.Year : null;
        }
    }
}
