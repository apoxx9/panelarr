using System;
using System.IO;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMoveComicFiles
    {
        ComicFile MoveComicFile(ComicFile comicFile, Series series);
        ComicFile MoveComicFile(ComicFile comicFile, LocalIssue localIssue);
        ComicFile CopyComicFile(ComicFile comicFile, LocalIssue localIssue);
    }

    public class ComicFileMovingService : IMoveComicFiles
    {
        private readonly IIssueService _issueService;
        private readonly IUpdateComicFileService _updateComicFileService;
        private readonly IBuildFileNames _buildFileNames;
        private readonly IDiskTransferService _diskTransferService;
        private readonly IDiskProvider _diskProvider;
        private readonly IRootFolderWatchingService _rootFolderWatchingService;
        private readonly IMediaFileAttributeService _mediaFileAttributeService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public ComicFileMovingService(IIssueService issueService,
                                      IUpdateComicFileService updateComicFileService,
                                      IBuildFileNames buildFileNames,
                                      IDiskTransferService diskTransferService,
                                      IDiskProvider diskProvider,
                                      IRootFolderWatchingService rootFolderWatchingService,
                                      IMediaFileAttributeService mediaFileAttributeService,
                                      IEventAggregator eventAggregator,
                                      IConfigService configService,
                                      Logger logger)
        {
            _issueService = issueService;
            _updateComicFileService = updateComicFileService;
            _buildFileNames = buildFileNames;
            _diskTransferService = diskTransferService;
            _diskProvider = diskProvider;
            _rootFolderWatchingService = rootFolderWatchingService;
            _mediaFileAttributeService = mediaFileAttributeService;
            _eventAggregator = eventAggregator;
            _configService = configService;
            _logger = logger;
        }

        public ComicFile MoveComicFile(ComicFile comicFile, Series series)
        {
            var issue = _issueService.GetIssue(comicFile.IssueId);
            var newFileName = _buildFileNames.BuildComicFileName(series, issue, comicFile);
            var filePath = _buildFileNames.BuildComicFilePath(series, issue, newFileName, Path.GetExtension(comicFile.Path));

            EnsureIssueFolder(comicFile, series, issue, filePath);

            _logger.Debug("Renaming issue file: {0} to {1}", comicFile, filePath);

            return TransferFile(comicFile, series, issue, filePath, TransferMode.Move);
        }

        public ComicFile MoveComicFile(ComicFile comicFile, LocalIssue localIssue)
        {
            var newFileName = _buildFileNames.BuildComicFileName(localIssue.Series, localIssue.Issue, comicFile);
            var filePath = _buildFileNames.BuildComicFilePath(localIssue.Series, localIssue.Issue, newFileName, Path.GetExtension(localIssue.Path));

            EnsureIssueFolder(comicFile, localIssue, filePath);

            _logger.Debug("Moving issue file: {0} to {1}", comicFile.Path, filePath);

            return TransferFile(comicFile, localIssue.Series, localIssue.Issue, filePath, TransferMode.Move);
        }

        public ComicFile CopyComicFile(ComicFile comicFile, LocalIssue localIssue)
        {
            var newFileName = _buildFileNames.BuildComicFileName(localIssue.Series, localIssue.Issue, comicFile);
            var filePath = _buildFileNames.BuildComicFilePath(localIssue.Series, localIssue.Issue, newFileName, Path.GetExtension(localIssue.Path));

            EnsureIssueFolder(comicFile, localIssue, filePath);

            if (_configService.CopyUsingHardlinks)
            {
                _logger.Debug("Hardlinking issue file: {0} to {1}", comicFile.Path, filePath);
                return TransferFile(comicFile, localIssue.Series, localIssue.Issue, filePath, TransferMode.HardLinkOrCopy);
            }

            _logger.Debug("Copying issue file: {0} to {1}", comicFile.Path, filePath);
            return TransferFile(comicFile, localIssue.Series, localIssue.Issue, filePath, TransferMode.Copy);
        }

        private ComicFile TransferFile(ComicFile comicFile, Series series, Issue issue, string destinationFilePath, TransferMode mode)
        {
            Ensure.That(comicFile, () => comicFile).IsNotNull();
            Ensure.That(series, () => series).IsNotNull();
            Ensure.That(destinationFilePath, () => destinationFilePath).IsValidPath(PathValidationType.CurrentOs);

            var comicFilePath = comicFile.Path;

            if (!_diskProvider.FileExists(comicFilePath))
            {
                throw new FileNotFoundException("Issue file path does not exist", comicFilePath);
            }

            if (comicFilePath == destinationFilePath)
            {
                throw new SameFilenameException("File not moved, source and destination are the same", comicFilePath);
            }

            _rootFolderWatchingService.ReportFileSystemChangeBeginning(comicFilePath, destinationFilePath);
            _diskTransferService.TransferFile(comicFilePath, destinationFilePath, mode);

            comicFile.Path = destinationFilePath;

            _updateComicFileService.ChangeFileDateForFile(comicFile, series, issue);

            try
            {
                _mediaFileAttributeService.SetFolderLastWriteTime(series.Path, comicFile.DateAdded);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to set last write time");
            }

            _mediaFileAttributeService.SetFilePermissions(destinationFilePath);

            return comicFile;
        }

        private void EnsureIssueFolder(ComicFile comicFile, LocalIssue localIssue, string filePath)
        {
            EnsureIssueFolder(comicFile, localIssue.Series, localIssue.Issue, filePath);
        }

        private void EnsureIssueFolder(ComicFile comicFile, Series series, Issue issue, string filePath)
        {
            var comicFileFolder = Path.GetDirectoryName(filePath);
            var issueFolder = _buildFileNames.BuildIssuePath(series);
            var seriesFolder = series.Path;
            var rootFolder = new OsPath(seriesFolder).Directory.FullPath;

            if (!_diskProvider.FolderExists(rootFolder))
            {
                throw new RootFolderNotFoundException(string.Format("Root folder '{0}' was not found.", rootFolder));
            }

            var changed = false;
            var newEvent = new IssueFolderCreatedEvent(series, comicFile);

            _rootFolderWatchingService.ReportFileSystemChangeBeginning(seriesFolder, issueFolder, comicFileFolder);

            if (!_diskProvider.FolderExists(seriesFolder))
            {
                CreateFolder(seriesFolder);
                newEvent.SeriesFolder = seriesFolder;
                changed = true;
            }

            if (seriesFolder != issueFolder && !_diskProvider.FolderExists(issueFolder))
            {
                CreateFolder(issueFolder);
                newEvent.IssueFolder = issueFolder;
                changed = true;
            }

            if (issueFolder != comicFileFolder && !_diskProvider.FolderExists(comicFileFolder))
            {
                CreateFolder(comicFileFolder);
                newEvent.ComicFileFolder = comicFileFolder;
                changed = true;
            }

            if (changed)
            {
                _eventAggregator.PublishEvent(newEvent);
            }
        }

        private void CreateFolder(string directoryName)
        {
            Ensure.That(directoryName, () => directoryName).IsNotNullOrWhiteSpace();

            var parentFolder = new OsPath(directoryName).Directory.FullPath;
            if (!_diskProvider.FolderExists(parentFolder))
            {
                CreateFolder(parentFolder);
            }

            try
            {
                _diskProvider.CreateFolder(directoryName);
            }
            catch (IOException ex)
            {
                _logger.Error(ex, "Unable to create directory: {0}", directoryName);
            }

            _mediaFileAttributeService.SetFolderPermissions(directoryName);
        }
    }
}
