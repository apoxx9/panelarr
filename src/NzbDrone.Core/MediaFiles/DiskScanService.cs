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
            if (folders == null)
            {
                folders = _rootFolderService.All().Select(x => x.Path).ToList();
            }

            if (seriesIds == null)
            {
                seriesIds = new List<int>();
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
                    _logger.Debug("Specified scan folder ({0}) doesn't exist.", folder);

                    CleanMediaFiles(folder, new List<string>());
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

            var decisionsStopwatch = Stopwatch.StartNew();

            var config = new ImportDecisionMakerConfig
            {
                Filter = filter,
                IncludeExisting = true,
                AddNewSeries = addNewSeries
            };

            var decisions = _importDecisionMaker.GetImportDecisions(mediaFileList, null, null, config);

            decisionsStopwatch.Stop();
            _logger.Debug("Import decisions complete [{0}]", decisionsStopwatch.Elapsed);

            var importStopwatch = Stopwatch.StartNew();
            _importApprovedIssues.Import(decisions, false);

            // decisions may have been filtered to just new files.  Anything new and approved will have been inserted.
            // Now we need to make sure anything new but not approved gets inserted
            // Note that knownFiles will include anything imported just now
            var knownFiles = new List<ComicFile>();
            folders.ForEach(x => knownFiles.AddRange(_mediaFileService.GetFilesWithBasePath(x)));

            var newFiles = decisions
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
                    MediaInfo = decision.Item.FileTrackInfo.MediaInfo,
                    IssueId = decision.Item.Issue?.Id ?? 0,
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
                    x.File.MediaInfo = x.Item.FileTrackInfo.MediaInfo;
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
