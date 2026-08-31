using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles
{
    public interface IDiskScanService
    {
        void Scan(List<string> folders = null, FilterFilesType filter = FilterFilesType.Known, bool addNewSeries = false, List<int> seriesIds = null);
        IFileInfo[] GetComicFiles(string path, bool allDirectories = true);
        string[] GetNonComicFiles(string path, bool allDirectories = true);
        List<IFileInfo> FilterFiles(string basePath, IEnumerable<IFileInfo> files);
        List<string> FilterPaths(string basePath, IEnumerable<string> paths);
    }

    public class DiskScanService :
        IDiskScanService,
        IExecute<RescanFoldersCommand>
    {
        public static readonly Regex ExcludedSubFoldersRegex = new Regex(@"(?:\\|\/|^)(?:extras|@eadir|extrafanart|plex versions|\.[^\\/]+)(?:\\|\/)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static readonly Regex ExcludedFilesRegex = new Regex(@"^\._|^Thumbs\.db$|^\.DS_store$|\.partial~$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IConfigService _configService;
        private readonly IDiskProvider _diskProvider;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMakeImportDecision _importDecisionMaker;
        private readonly IImportApprovedIssues _importApprovedIssues;
        private readonly ISeriesService _seriesService;
        private readonly IMediaFileTableCleanupService _mediaFileTableCleanupService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public DiskScanService(IConfigService configService,
                               IDiskProvider diskProvider,
                               IMediaFileService mediaFileService,
                               IMakeImportDecision importDecisionMaker,
                               IImportApprovedIssues importApprovedIssues,
                               ISeriesService seriesService,
                               IRootFolderService rootFolderService,
                               IMediaFileTableCleanupService mediaFileTableCleanupService,
                               IEventAggregator eventAggregator,
                               Logger logger)
        {
            _configService = configService;
            _diskProvider = diskProvider;
            _mediaFileService = mediaFileService;
            _importDecisionMaker = importDecisionMaker;
            _importApprovedIssues = importApprovedIssues;
            _seriesService = seriesService;
            _mediaFileTableCleanupService = mediaFileTableCleanupService;
            _rootFolderService = rootFolderService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public void Scan(List<string> folders = null, FilterFilesType filter = FilterFilesType.Known, bool addNewSeries = false, List<int> seriesIds = null)
        {
            if (seriesIds == null)
            {
                seriesIds = new List<int>();
            }

            // A scan for specific series walks THEIR folders. Falling back to
            // the root folders here turned an API rescan of one series into a
            // library-wide identification pass with that series forced as
            // the override - and re-homed another series' files into it.
            if (folders == null && seriesIds.Any())
            {
                folders = _seriesService.GetSeries(seriesIds)
                    .Where(s => s.Path.IsNotNullOrWhiteSpace())
                    .Select(s => s.Path)
                    .ToList();

                if (!folders.Any())
                {
                    _logger.Warn("None of the series {0} has a folder to scan", seriesIds.ConcatToString(", "));
                    return;
                }
            }

            if (folders == null)
            {
                folders = _rootFolderService.All().Select(x => x.Path).ToList();
            }

            var mediaFileList = new List<IFileInfo>();

            var comicFilesStopwatch = Stopwatch.StartNew();

            foreach (var folder in folders)
            {
                // We could be scanning a root folder or a subset of a root folder.  If it's a subset,
                // check if the root folder exists before cleaning.
                var rootFolder = _rootFolderService.GetBestRootFolder(folder);

                if (rootFolder == null)
                {
                    _logger.Error("Not scanning {0}, it's not a subdirectory of a defined root folder", folder);
                    return;
                }

                var folderExists = _diskProvider.FolderExists(folder);

                if (!folderExists)
                {
                    if (!_diskProvider.FolderExists(rootFolder.Path))
                    {
                        _logger.Warn("Series root folder ({0}) doesn't exist.", rootFolder.Path);
                        var skippedSeries = _seriesService.GetSeries(seriesIds);
                        skippedSeries.ForEach(x => _eventAggregator.PublishEvent(new SeriesScanSkippedEvent(x, SeriesScanSkippedReason.RootFolderDoesNotExist)));
                        return;
                    }

                    if (_diskProvider.FolderEmpty(rootFolder.Path))
                    {
                        _logger.Warn("Series root folder ({0}) is empty.", rootFolder.Path);
                        var skippedSeries = _seriesService.GetSeries(seriesIds);
                        skippedSeries.ForEach(x => _eventAggregator.PublishEvent(new SeriesScanSkippedEvent(x, SeriesScanSkippedReason.RootFolderIsEmpty)));
                        return;
                    }
                }

                if (!folderExists)
                {
                    // Vanished-mount guard: a folder that suddenly stops
                    // existing is far more likely a broken mount than a
                    // deliberate deletion — purging its rows here destroyed
                    // ~2,490 of them during the 2026-07-08 NFS race. Keep the
                    // rows; an existing-but-empty folder is skipped below for
                    // the same reason.
                    _logger.Warn("Series folder {0} doesn't exist - skipping DB cleanup for its files (vanished-mount guard)", folder);
                    continue;
                }

                _logger.ProgressInfo("Scanning {0}", folder);

                var files = FilterFiles(folder, GetComicFiles(folder));

                if (!files.Any())
                {
                    _logger.Warn("Scan folder {0} is empty.", folder);
                    continue;
                }

                CleanMediaFiles(folder, files.Select(x => x.FullName).ToList());
                mediaFileList.AddRange(files);
            }

            comicFilesStopwatch.Stop();
            _logger.Trace("Finished getting comic files for:\n{0} [{1}]", folders.ConcatToString("\n"), comicFilesStopwatch.Elapsed);

            // Nothing visited means nothing to decide. Running identification
            // anyway - with the series forced as an override - handed a
            // per-series rescan of a not-yet-existing folder the whole
            // library's unmapped files, and it re-homed two of another
            // series' issues into the forced series.
            if (!mediaFileList.Any())
            {
                _logger.Debug("No comic files found under {0}, nothing to import", folders.ConcatToString(", "));
                return;
            }

            var decisionsStopwatch = Stopwatch.StartNew();

            var config = new ImportDecisionMakerConfig
            {
                Filter = filter,
                IncludeExisting = true,
                AddNewSeries = addNewSeries
            };

            // When scanning for a specific series, pass it as an override so the
            // identification service can match files directly against that series'
            // issues instead of relying solely on filename parsing.
            IdentificationOverrides idOverrides = null;
            if (seriesIds.Count == 1)
            {
                var series = _seriesService.GetSeries(seriesIds.First());
                if (series != null)
                {
                    idOverrides = new IdentificationOverrides { Series = series };
                }
            }

            var decisions = _importDecisionMaker.GetImportDecisions(mediaFileList, idOverrides, null, config);

            decisionsStopwatch.Stop();
            _logger.Debug("Import decisions complete [{0}]", decisionsStopwatch.Elapsed);

            var importStopwatch = Stopwatch.StartNew();

            // A scan without series context can fuzzy-match files of a
            // name-similar series ("Power Rangers" vs "Power Rangers Prime"),
            // so only exact tag-id identifications and files inside a series'
            // own folder are trusted for automatic import. Everything else is
            // persisted as unmapped below and left for Library Import review.
            // A scan may only import what it visited: identification folds a
            // candidate's existing files (under OTHER series' folders) into
            // the decision list as context, and importing those re-homes
            // another series' files
            var importableDecisions = decisions
                .Where(x => folders.Any(f => f.IsParentPath(x.Item.Path)))
                .ToList();

            if (!seriesIds.Any())
            {
                var seriesPaths = _seriesService.AllSeriesPaths().Values.ToList();

                importableDecisions = importableDecisions
                    .Where(x => x.Item.ExactTagMatch ||
                                seriesPaths.Any(p => p.IsParentPath(x.Item.Path)))
                    .ToList();

                var heldBack = decisions.Count - importableDecisions.Count;

                if (heldBack > 0)
                {
                    _logger.Debug("Holding back {0} fuzzy-matched files outside series folders for Library Import review", heldBack);
                }
            }

            _importApprovedIssues.Import(importableDecisions, false);

            // decisions may have been filtered to just new files.  Anything new and approved will have been inserted.
            // Now we need to make sure anything new but not approved gets inserted
            // Note that knownFiles will include anything imported just now
            var knownFiles = new List<ComicFile>();
            folders.ForEach(x => knownFiles.AddRange(_mediaFileService.GetFilesWithBasePath(x)));

            var newFiles = decisions

                // Identification pulls a candidate's existing files (living
                // under OTHER series' folders) into the decision list as
                // context; only files this scan actually visited may be
                // inserted, and only once per path
                .Where(x => folders.Any(f => f.IsParentPath(x.Item.Path)))
                .DistinctBy(x => x.Item.Path)
                .ExceptBy(x => x.Item.Path, knownFiles, x => x.Path, PathEqualityComparer.Instance)
                .Select(decision => new ComicFile
                {
                    Path = decision.Item.Path,
                    Part = decision.Item.Part,
                    PartCount = decision.Item.PartCount,
                    Size = decision.Item.Size,
                    Modified = decision.Item.Modified,
                    DateAdded = DateTime.UtcNow,
                    Quality = decision.Item.Quality,

                    // Everything left here was NOT imported (rejected or
                    // unidentified). Persisting a rejected file with a live
                    // IssueId would make it look like the issue's file and
                    // exclude it from re-evaluation on future rescans.
                    IssueId = 0,
                    ComicFormat = GetComicFormat(decision.Item.Path)
                })
                .ToList();
            _mediaFileService.AddMany(newFiles);

            _logger.Debug($"Inserted {newFiles.Count} new unmatched comic files");

            // finally update info on size/modified for existing files
            var updatedFiles = knownFiles
                .Join(decisions,
                      x => x.Path,
                      x => x.Item.Path,
                      (file, decision) => new
                      {
                          File = file,
                          Item = decision.Item
                      },
                      PathEqualityComparer.Instance)
                .Where(x => x.File.Size != x.Item.Size ||
                       Math.Abs((x.File.Modified - x.Item.Modified).TotalSeconds) > 1)
                .Select(x =>
                {
                    x.File.Size = x.Item.Size;
                    x.File.Modified = x.Item.Modified;
                    x.File.Quality = x.Item.Quality;
                    return x.File;
                })
                .ToList();

            _mediaFileService.Update(updatedFiles);

            _logger.Debug($"Updated info for {updatedFiles.Count} known files");

            var seriesList = _seriesService.GetSeries(seriesIds);
            foreach (var series in seriesList)
            {
                CompletedScanning(series);
            }

            importStopwatch.Stop();
            _logger.Debug("Issue import complete for:\n{0} [{1}]", folders.ConcatToString("\n"), importStopwatch.Elapsed);
        }

        private void CleanMediaFiles(string folder, List<string> mediaFileList)
        {
            _logger.Debug($"Cleaning up media files in DB [{folder}]");
            _mediaFileTableCleanupService.Clean(folder, mediaFileList);
        }

        private void CompletedScanning(Series series)
        {
            _logger.Info("Completed scanning disk for {0}", series.Name);
            _eventAggregator.PublishEvent(new SeriesScannedEvent(series));
        }

        public IFileInfo[] GetComicFiles(string path, bool allDirectories = true)
        {
            IEnumerable<IFileInfo> filesOnDisk;

            var rootFolder = _rootFolderService.GetBestRootFolder(path);

            _logger.Trace(rootFolder.ToJson());

            _logger.Debug("Scanning '{0}' for comic files", path);

            filesOnDisk = _diskProvider.GetFileInfos(path, allDirectories);

            _logger.Trace("{0} files were found in {1}", filesOnDisk.Count(), path);

            var mediaFileList = filesOnDisk.Where(file => MediaFileExtensions.AllExtensions.Contains(file.Extension))
                .ToArray();

            _logger.Debug("{0} issue files were found in {1}", mediaFileList.Length, path);

            return mediaFileList;
        }

        public string[] GetNonComicFiles(string path, bool allDirectories = true)
        {
            _logger.Debug("Scanning '{0}' for non-comic files", path);

            var filesOnDisk = _diskProvider.GetFiles(path, allDirectories).ToList();

            var mediaFileList = filesOnDisk.Where(file => !MediaFileExtensions.AllExtensions.Contains(Path.GetExtension(file)))
                                           .ToList();

            _logger.Trace("{0} files were found in {1}", filesOnDisk.Count, path);
            _logger.Debug("{0} non-comic files were found in {1}", mediaFileList.Count, path);

            return mediaFileList.ToArray();
        }

        public List<string> FilterPaths(string basePath, IEnumerable<string> paths)
        {
            return paths.Where(file => !ExcludedSubFoldersRegex.IsMatch(basePath.GetRelativePath(file)))
                        .Where(file => !ExcludedFilesRegex.IsMatch(Path.GetFileName(file)))
                        .ToList();
        }

        public List<IFileInfo> FilterFiles(string basePath, IEnumerable<IFileInfo> files)
        {
            return files.Where(file => !ExcludedSubFoldersRegex.IsMatch(basePath.GetRelativePath(file.FullName)))
                        .Where(file => !ExcludedFilesRegex.IsMatch(file.Name))
                        .ToList();
        }

        private static Issues.ComicFormat GetComicFormat(string path)
        {
            var ext = Path.GetExtension(path)?.TrimStart('.').ToLowerInvariant();
            return ext switch
            {
                "cbz" => Issues.ComicFormat.CBZ,
                "cbr" => Issues.ComicFormat.CBR,
                "cb7" => Issues.ComicFormat.CB7,
                "pdf" => Issues.ComicFormat.PDF,
                _ => Issues.ComicFormat.Unknown
            };
        }

        public void Execute(RescanFoldersCommand message)
        {
            Scan(message.Folders, message.Filter, message.AddNewSeries, message.SeriesIds);
        }
    }
}
