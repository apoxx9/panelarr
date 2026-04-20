using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public interface IDownloadedIssuesImportService
    {
        List<ImportResult> ProcessRootFolder(IDirectoryInfo directoryInfo);
        List<ImportResult> ProcessPath(string path, ImportMode importMode = ImportMode.Auto, Series series = null, DownloadClientItem downloadClientItem = null);
        bool ShouldDeleteFolder(IDirectoryInfo directoryInfo);
    }

    public class DownloadedIssuesImportService : IDownloadedIssuesImportService
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IDiskScanService _diskScanService;
        private readonly ISeriesService _seriesService;
        private readonly IParsingService _parsingService;
        private readonly IMakeImportDecision _importDecisionMaker;
        private readonly IImportApprovedIssues _importApprovedIssues;
        private readonly IEventAggregator _eventAggregator;
        private readonly IRuntimeInfo _runtimeInfo;
        private readonly Logger _logger;

        public DownloadedIssuesImportService(IDiskProvider diskProvider,
                                             IDiskScanService diskScanService,
                                             ISeriesService seriesService,
                                             IParsingService parsingService,
                                             IMakeImportDecision importDecisionMaker,
                                             IImportApprovedIssues importApprovedIssues,
                                             IEventAggregator eventAggregator,
                                             IRuntimeInfo runtimeInfo,
                                             Logger logger)
        {
            _diskProvider = diskProvider;
            _diskScanService = diskScanService;
            _seriesService = seriesService;
            _parsingService = parsingService;
            _importDecisionMaker = importDecisionMaker;
            _importApprovedIssues = importApprovedIssues;
            _eventAggregator = eventAggregator;
            _runtimeInfo = runtimeInfo;
            _logger = logger;
        }

        public List<ImportResult> ProcessRootFolder(IDirectoryInfo directoryInfo)
        {
            var results = new List<ImportResult>();

            foreach (var subFolder in _diskProvider.GetDirectoryInfos(directoryInfo.FullName))
            {
                var folderResults = ProcessFolder(subFolder, ImportMode.Auto, null);
                results.AddRange(folderResults);
            }

            foreach (var comicFile in _diskScanService.GetComicFiles(directoryInfo.FullName, false))
            {
                var fileResults = ProcessFile(comicFile, ImportMode.Auto, null);
                results.AddRange(fileResults);
            }

            return results;
        }

        public List<ImportResult> ProcessPath(string path, ImportMode importMode = ImportMode.Auto, Series series = null, DownloadClientItem downloadClientItem = null)
        {
            _logger.Debug("Processing path: {0}", path);

            if (_diskProvider.FolderExists(path))
            {
                var directoryInfo = _diskProvider.GetDirectoryInfo(path);

                if (series == null)
                {
                    return ProcessFolder(directoryInfo, importMode, downloadClientItem);
                }

                return ProcessFolder(directoryInfo, importMode, series, downloadClientItem);
            }

            if (_diskProvider.FileExists(path))
            {
                var fileInfo = _diskProvider.GetFileInfo(path);

                if (series == null)
                {
                    return ProcessFile(fileInfo, importMode, downloadClientItem);
                }

                return ProcessFile(fileInfo, importMode, series, downloadClientItem);
            }

            LogInaccessiblePathError(path);
            _eventAggregator.PublishEvent(new TrackImportFailedEvent(null, null, true, downloadClientItem));

            return new List<ImportResult>();
        }

        public bool ShouldDeleteFolder(IDirectoryInfo directoryInfo)
        {
            try
            {
                var comicFiles = _diskScanService.GetComicFiles(directoryInfo.FullName);
                var rarFiles = _diskProvider.GetFiles(directoryInfo.FullName, true).Where(f =>
                    Path.GetExtension(f).Equals(".rar",
                        StringComparison.OrdinalIgnoreCase));

                foreach (var comicFile in comicFiles)
                {
                    var parseResult = Parser.Parser.ParseTitle(comicFile.Name);

                    if (parseResult == null)
                    {
                        _logger.Warn("Unable to parse file on import: [{0}]", comicFile);
                        return false;
                    }

                    _logger.Warn("Issue file detected: [{0}]", comicFile);
                    return false;
                }

                if (rarFiles.Any(f => _diskProvider.GetFileSize(f) > 10.Megabytes()))
                {
                    _logger.Warn("RAR file detected, will require manual cleanup");
                    return false;
                }

                return true;
            }
            catch (DirectoryNotFoundException e)
            {
                _logger.Debug(e, "Folder {0} has already been removed", directoryInfo.FullName);
                return false;
            }
            catch (Exception e)
            {
                _logger.Debug(e, "Unable to determine whether folder {0} should be removed", directoryInfo.FullName);
                return false;
            }
        }

        private List<ImportResult> ProcessFolder(IDirectoryInfo directoryInfo, ImportMode importMode, DownloadClientItem downloadClientItem)
        {
            var cleanedUpName = GetCleanedUpFolderName(directoryInfo.Name);
            var series = _parsingService.GetSeries(cleanedUpName);

            return ProcessFolder(directoryInfo, importMode, series, downloadClientItem);
        }

        private List<ImportResult> ProcessFolder(IDirectoryInfo directoryInfo, ImportMode importMode, Series series, DownloadClientItem downloadClientItem)
        {
            if (_seriesService.SeriesPathExists(directoryInfo.FullName))
            {
                _logger.Warn("Unable to process folder that is mapped to an existing series");
                return new List<ImportResult>();
            }

            var cleanedUpName = GetCleanedUpFolderName(directoryInfo.Name);
            var folderInfo = Parser.Parser.ParseIssueTitle(directoryInfo.Name);
            var trackInfo = new ParsedTrackInfo { };

            if (folderInfo != null)
            {
                _logger.Debug("{0} folder quality: {1}", cleanedUpName, folderInfo.Quality);

                trackInfo = new ParsedTrackInfo
                {
                    IssueTitle = folderInfo.IssueTitle,
                    Series = new List<string> { folderInfo.SeriesName },
                    Quality = folderInfo.Quality,
                    ReleaseGroup = folderInfo.ReleaseGroup,
                    ReleaseHash = folderInfo.ReleaseHash,
                };
            }
            else
            {
                trackInfo = null;
            }

            var comicFiles = _diskScanService.FilterFiles(directoryInfo.FullName, _diskScanService.GetComicFiles(directoryInfo.FullName));

            if (downloadClientItem == null)
            {
                foreach (var comicFile in comicFiles)
                {
                    if (_diskProvider.IsFileLocked(comicFile.FullName))
                    {
                        return new List<ImportResult>
                               {
                                   FileIsLockedResult(comicFile.FullName)
                               };
                    }
                }
            }

            var idOverrides = new IdentificationOverrides
            {
                Series = series
            };
            var idInfo = new ImportDecisionMakerInfo
            {
                DownloadClientItem = downloadClientItem,
                ParsedIssueInfo = folderInfo
            };
            var idConfig = new ImportDecisionMakerConfig
            {
                Filter = FilterFilesType.None,
                NewDownload = true,
                SingleRelease = false,
                IncludeExisting = false,
                AddNewSeries = false
            };

            var decisions = _importDecisionMaker.GetImportDecisions(comicFiles, idOverrides, idInfo, idConfig);
            var importResults = _importApprovedIssues.Import(decisions, true, downloadClientItem, importMode);

            if (importMode == ImportMode.Auto)
            {
                importMode = (downloadClientItem == null || downloadClientItem.CanMoveFiles) ? ImportMode.Move : ImportMode.Copy;
            }

            if (importMode == ImportMode.Move &&
                importResults.Any(i => i.Result == ImportResultType.Imported) &&
                ShouldDeleteFolder(directoryInfo))
            {
                _logger.Debug("Deleting folder after importing valid files");

                try
                {
                    _diskProvider.DeleteFolder(directoryInfo.FullName, true);
                }
                catch (IOException e)
                {
                    _logger.Debug(e, "Unable to delete folder after importing: {0}", e.Message);
                }
            }

            return importResults;
        }

        private List<ImportResult> ProcessFile(IFileInfo fileInfo, ImportMode importMode, DownloadClientItem downloadClientItem)
        {
            var series = _parsingService.GetSeries(Path.GetFileNameWithoutExtension(fileInfo.Name));

            if (series == null)
            {
                _logger.Debug("Unknown Series for file: {0}", fileInfo.Name);

                return new List<ImportResult>
                       {
                           UnknownSeriesResult(string.Format("Unknown Series for file: {0}", fileInfo.Name), fileInfo.FullName)
                       };
            }

            return ProcessFile(fileInfo, importMode, series, downloadClientItem);
        }

        private List<ImportResult> ProcessFile(IFileInfo fileInfo, ImportMode importMode, Series series, DownloadClientItem downloadClientItem)
        {
            if (Path.GetFileNameWithoutExtension(fileInfo.Name).StartsWith("._"))
            {
                _logger.Debug("[{0}] starts with '._', skipping", fileInfo.FullName);

                return new List<ImportResult>
                       {
                           new ImportResult(new ImportDecision<LocalIssue>(new LocalIssue { Path = fileInfo.FullName }, new Rejection("Invalid music file, filename starts with '._'")), "Invalid music file, filename starts with '._'")
                       };
            }

            if (downloadClientItem == null)
            {
                if (_diskProvider.IsFileLocked(fileInfo.FullName))
                {
                    return new List<ImportResult>
                           {
                               FileIsLockedResult(fileInfo.FullName)
                           };
                }
            }

            var idOverrides = new IdentificationOverrides
            {
                Series = series
            };
            var idInfo = new ImportDecisionMakerInfo
            {
                DownloadClientItem = downloadClientItem
            };
            var idConfig = new ImportDecisionMakerConfig
            {
                Filter = FilterFilesType.None,
                NewDownload = true,
                SingleRelease = false,
                IncludeExisting = false,
                AddNewSeries = false
            };

            var decisions = _importDecisionMaker.GetImportDecisions(new List<IFileInfo>() { fileInfo }, idOverrides, idInfo, idConfig);

            return _importApprovedIssues.Import(decisions, true, downloadClientItem, importMode);
        }

        private string GetCleanedUpFolderName(string folder)
        {
            folder = folder.Replace("_UNPACK_", "")
                           .Replace("_FAILED_", "");

            return folder;
        }

        private ImportResult FileIsLockedResult(string comicFile)
        {
            _logger.Debug("[{0}] is currently locked by another process, skipping", comicFile);
            return new ImportResult(new ImportDecision<LocalIssue>(new LocalIssue { Path = comicFile }, new Rejection("Locked file, try again later")), "Locked file, try again later");
        }

        private ImportResult UnknownSeriesResult(string message, string comicFile = null)
        {
            var localTrack = comicFile == null ? null : new LocalIssue { Path = comicFile };

            return new ImportResult(new ImportDecision<LocalIssue>(localTrack, new Rejection("Unknown Series")), message);
        }

        private void LogInaccessiblePathError(string path)
        {
            if (_runtimeInfo.IsWindowsService)
            {
                var mounts = _diskProvider.GetMounts();
                var mount = mounts.FirstOrDefault(m => m.RootDirectory == Path.GetPathRoot(path));

                if (mount == null)
                {
                    _logger.Error("Import failed, path does not exist or is not accessible by Panelarr: {0}. Unable to find a volume mounted for the path. If you're using a mapped network drive see the FAQ for more info", path);
                    return;
                }

                if (mount.DriveType == DriveType.Network)
                {
                    _logger.Error("Import failed, path does not exist or is not accessible by Panelarr: {0}. It's recommended to avoid mapped network drives when running as a Windows service. See the FAQ for more info", path);
                    return;
                }
            }

            if (OsInfo.IsWindows)
            {
                if (path.StartsWith(@"\\"))
                {
                    _logger.Error("Import failed, path does not exist or is not accessible by Panelarr: {0}. Ensure the user running Panelarr has access to the network share", path);
                    return;
                }
            }

            _logger.Error("Import failed, path does not exist or is not accessible by Panelarr: {0}. Ensure the path exists and the user running Panelarr has the correct permissions to access this file/folder", path);
        }
    }
}
