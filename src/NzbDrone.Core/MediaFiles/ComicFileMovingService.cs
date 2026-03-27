using System;
using System.IO;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Core.Books;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMoveBookFiles
    {
        ComicFile MoveBookFile(ComicFile comicFile, Series author);
        ComicFile MoveBookFile(ComicFile comicFile, LocalBook localBook);
        ComicFile CopyBookFile(ComicFile comicFile, LocalBook localBook);
    }

    public class ComicFileMovingService : IMoveBookFiles
    {
        private readonly IBookService _bookService;
        private readonly IUpdateBookFileService _updateBookFileService;
        private readonly IBuildFileNames _buildFileNames;
        private readonly IDiskTransferService _diskTransferService;
        private readonly IDiskProvider _diskProvider;
        private readonly IRootFolderWatchingService _rootFolderWatchingService;
        private readonly IMediaFileAttributeService _mediaFileAttributeService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IConfigService _configService;
        private readonly Logger _logger;

        public ComicFileMovingService(IBookService bookService,
                                      IUpdateBookFileService updateBookFileService,
                                      IBuildFileNames buildFileNames,
                                      IDiskTransferService diskTransferService,
                                      IDiskProvider diskProvider,
                                      IRootFolderWatchingService rootFolderWatchingService,
                                      IMediaFileAttributeService mediaFileAttributeService,
                                      IEventAggregator eventAggregator,
                                      IConfigService configService,
                                      Logger logger)
        {
            _bookService = bookService;
            _updateBookFileService = updateBookFileService;
            _buildFileNames = buildFileNames;
            _diskTransferService = diskTransferService;
            _diskProvider = diskProvider;
            _rootFolderWatchingService = rootFolderWatchingService;
            _mediaFileAttributeService = mediaFileAttributeService;
            _eventAggregator = eventAggregator;
            _configService = configService;
            _logger = logger;
        }

        public ComicFile MoveBookFile(ComicFile comicFile, Series author)
        {
            var issue = _bookService.GetBook(comicFile.IssueId);
            var newFileName = _buildFileNames.BuildBookFileName(author, issue, comicFile);
            var filePath = _buildFileNames.BuildBookFilePath(author, issue, newFileName, Path.GetExtension(comicFile.Path));

            EnsureBookFolder(comicFile, author, issue, filePath);

            _logger.Debug("Renaming issue file: {0} to {1}", comicFile, filePath);

            return TransferFile(comicFile, author, issue, filePath, TransferMode.Move);
        }

        public ComicFile MoveBookFile(ComicFile comicFile, LocalBook localBook)
        {
            var newFileName = _buildFileNames.BuildBookFileName(localBook.Series, localBook.Issue, comicFile);
            var filePath = _buildFileNames.BuildBookFilePath(localBook.Series, localBook.Issue, newFileName, Path.GetExtension(localBook.Path));

            EnsureTrackFolder(comicFile, localBook, filePath);

            _logger.Debug("Moving issue file: {0} to {1}", comicFile.Path, filePath);

            return TransferFile(comicFile, localBook.Series, localBook.Issue, filePath, TransferMode.Move);
        }

        public ComicFile CopyBookFile(ComicFile comicFile, LocalBook localBook)
        {
            var newFileName = _buildFileNames.BuildBookFileName(localBook.Series, localBook.Issue, comicFile);
            var filePath = _buildFileNames.BuildBookFilePath(localBook.Series, localBook.Issue, newFileName, Path.GetExtension(localBook.Path));

            EnsureTrackFolder(comicFile, localBook, filePath);

            if (_configService.CopyUsingHardlinks)
            {
                _logger.Debug("Hardlinking issue file: {0} to {1}", comicFile.Path, filePath);
                return TransferFile(comicFile, localBook.Series, localBook.Issue, filePath, TransferMode.HardLinkOrCopy);
            }

            _logger.Debug("Copying issue file: {0} to {1}", comicFile.Path, filePath);
            return TransferFile(comicFile, localBook.Series, localBook.Issue, filePath, TransferMode.Copy);
        }

        private ComicFile TransferFile(ComicFile comicFile, Series author, Issue issue, string destinationFilePath, TransferMode mode)
        {
            Ensure.That(comicFile, () => comicFile).IsNotNull();
            Ensure.That(author, () => author).IsNotNull();
            Ensure.That(destinationFilePath, () => destinationFilePath).IsValidPath(PathValidationType.CurrentOs);

            var bookFilePath = comicFile.Path;

            if (!_diskProvider.FileExists(bookFilePath))
            {
                throw new FileNotFoundException("Issue file path does not exist", bookFilePath);
            }

            if (bookFilePath == destinationFilePath)
            {
                throw new SameFilenameException("File not moved, source and destination are the same", bookFilePath);
            }

            _rootFolderWatchingService.ReportFileSystemChangeBeginning(bookFilePath, destinationFilePath);
            _diskTransferService.TransferFile(bookFilePath, destinationFilePath, mode);

            comicFile.Path = destinationFilePath;

            _updateBookFileService.ChangeFileDateForFile(comicFile, author, issue);

            try
            {
                _mediaFileAttributeService.SetFolderLastWriteTime(author.Path, comicFile.DateAdded);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Unable to set last write time");
            }

            _mediaFileAttributeService.SetFilePermissions(destinationFilePath);

            return comicFile;
        }

        private void EnsureTrackFolder(ComicFile comicFile, LocalBook localBook, string filePath)
        {
            EnsureBookFolder(comicFile, localBook.Series, localBook.Issue, filePath);
        }

        private void EnsureBookFolder(ComicFile comicFile, Series author, Issue issue, string filePath)
        {
            var trackFolder = Path.GetDirectoryName(filePath);
            var bookFolder = _buildFileNames.BuildBookPath(author);
            var authorFolder = author.Path;
            var rootFolder = new OsPath(authorFolder).Directory.FullPath;

            if (!_diskProvider.FolderExists(rootFolder))
            {
                throw new RootFolderNotFoundException(string.Format("Root folder '{0}' was not found.", rootFolder));
            }

            var changed = false;
            var newEvent = new TrackFolderCreatedEvent(author, comicFile);

            _rootFolderWatchingService.ReportFileSystemChangeBeginning(authorFolder, bookFolder, trackFolder);

            if (!_diskProvider.FolderExists(authorFolder))
            {
                CreateFolder(authorFolder);
                newEvent.SeriesFolder = authorFolder;
                changed = true;
            }

            if (authorFolder != bookFolder && !_diskProvider.FolderExists(bookFolder))
            {
                CreateFolder(bookFolder);
                newEvent.IssueFolder = bookFolder;
                changed = true;
            }

            if (bookFolder != trackFolder && !_diskProvider.FolderExists(trackFolder))
            {
                CreateFolder(trackFolder);
                newEvent.TrackFolder = trackFolder;
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
