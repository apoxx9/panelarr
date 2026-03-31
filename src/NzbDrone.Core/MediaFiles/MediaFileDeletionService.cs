using System;
using System.Net;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.MediaFiles
{
    public interface IDeleteMediaFiles
    {
        void DeleteTrackFile(Series author, ComicFile comicFile);
        void DeleteTrackFile(ComicFile comicFile, string subfolder = "");
    }

    public class MediaFileDeletionService : IDeleteMediaFiles,
                                            IHandle<SeriesDeletedEvent>,
                                            IHandleAsync<SeriesDeletedEvent>,
                                            IHandleAsync<IssueDeletedEvent>,
                                            IHandle<ComicFileDeletedEvent>
    {
        private readonly IDiskProvider _diskProvider;
        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IMediaFileService _mediaFileService;
        private readonly ISeriesService _authorService;
        private readonly IConfigService _configService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public MediaFileDeletionService(IDiskProvider diskProvider,
                                        IRecycleBinProvider recycleBinProvider,
                                        IMediaFileService mediaFileService,
                                        ISeriesService authorService,
                                        IConfigService configService,
                                        IEventAggregator eventAggregator,
                                        Logger logger)
        {
            _diskProvider = diskProvider;
            _recycleBinProvider = recycleBinProvider;
            _mediaFileService = mediaFileService;
            _authorService = authorService;
            _configService = configService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public void DeleteTrackFile(Series author, ComicFile comicFile)
        {
            var fullPath = comicFile.Path;
            var rootFolder = _diskProvider.GetParentFolder(author.Path);

            if (!_diskProvider.FolderExists(rootFolder))
            {
                _logger.Warn("Series's root folder ({0}) doesn't exist.", rootFolder);
                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Series's root folder ({0}) doesn't exist.", rootFolder);
            }

            if (_diskProvider.GetDirectories(rootFolder).Empty())
            {
                _logger.Warn("Series's root folder ({0}) is empty.", rootFolder);
                throw new NzbDroneClientException(HttpStatusCode.Conflict, "Series's root folder ({0}) is empty.", rootFolder);
            }

            if (_diskProvider.FolderExists(author.Path))
            {
                var subfolder = _diskProvider.GetParentFolder(author.Path).GetRelativePath(_diskProvider.GetParentFolder(fullPath));
                DeleteTrackFile(comicFile, subfolder);
            }
            else
            {
                // delete from db even if the author folder is missing
                _mediaFileService.Delete(comicFile, DeleteMediaFileReason.Manual);
            }
        }

        public void DeleteTrackFile(ComicFile comicFile, string subfolder = "")
        {
            var fullPath = comicFile.Path;

            if (_diskProvider.FileExists(fullPath))
            {
                _logger.Info("Deleting issue file: {0}", fullPath);
                DeleteFile(comicFile, subfolder);
            }

            // Delete the track file from the database to clean it up even if the file was already deleted
            _mediaFileService.Delete(comicFile, DeleteMediaFileReason.Manual);

            _eventAggregator.PublishEvent(new DeleteCompletedEvent());
        }

        private void DeleteFile(ComicFile comicFile, string subfolder = "")
        {
            try
            {
                _recycleBinProvider.DeleteFile(comicFile.Path, subfolder);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Unable to delete issue file");
                throw new NzbDroneClientException(HttpStatusCode.InternalServerError, "Unable to delete issue file");
            }
        }

        [EventHandleOrder(EventHandleOrder.First)]
        public void Handle(SeriesDeletedEvent message)
        {
            // No Calibre-specific handling needed; file deletion is handled in HandleAsync
        }

        public void HandleAsync(SeriesDeletedEvent message)
        {
            if (message.DeleteFiles)
            {
                var author = message.Series;
                var allSeries = _authorService.AllSeriesPaths();

                foreach (var s in allSeries)
                {
                    if (s.Key == author.Id)
                    {
                        continue;
                    }

                    if (author.Path.IsParentPath(s.Value))
                    {
                        _logger.Error("Series path: '{0}' is a parent of another author, not deleting files.", author.Path);
                        return;
                    }

                    if (author.Path.PathEquals(s.Value))
                    {
                        _logger.Error("Series path: '{0}' is the same as another author, not deleting files.", author.Path);
                        return;
                    }
                }

                if (_diskProvider.FolderExists(message.Series.Path))
                {
                    _recycleBinProvider.DeleteFolder(message.Series.Path);
                }

                _eventAggregator.PublishEvent(new DeleteCompletedEvent());
            }
        }

        public void HandleAsync(IssueDeletedEvent message)
        {
            if (message.DeleteFiles)
            {
                var files = _mediaFileService.GetFilesByIssue(message.Issue.Id);
                foreach (var file in files)
                {
                    DeleteFile(file);
                }
            }
        }

        [EventHandleOrder(EventHandleOrder.Last)]
        public void Handle(ComicFileDeletedEvent message)
        {
            if (message.Reason == DeleteMediaFileReason.Upgrade)
            {
                return;
            }

            if (_configService.DeleteEmptyFolders)
            {
                var author = message.ComicFile.Series.Value;
                var bookFolder = message.ComicFile.Path.GetParentPath();

                if (_diskProvider.GetFiles(author.Path, true).Empty())
                {
                    _diskProvider.DeleteFolder(author.Path, true);
                }
                else if (_diskProvider.GetFiles(bookFolder, true).Empty())
                {
                    _diskProvider.RemoveEmptySubfolders(bookFolder);
                }
            }
        }
    }
}
