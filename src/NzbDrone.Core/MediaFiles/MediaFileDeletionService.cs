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
        void DeleteComicFile(Series series, ComicFile comicFile);
        void DeleteComicFile(ComicFile comicFile, string subfolder = "");
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
        private readonly ISeriesService _seriesService;
        private readonly IConfigService _configService;
        private readonly IEventAggregator _eventAggregator;
        private readonly Logger _logger;

        public MediaFileDeletionService(IDiskProvider diskProvider,
                                        IRecycleBinProvider recycleBinProvider,
                                        IMediaFileService mediaFileService,
                                        ISeriesService seriesService,
                                        IConfigService configService,
                                        IEventAggregator eventAggregator,
                                        Logger logger)
        {
            _diskProvider = diskProvider;
            _recycleBinProvider = recycleBinProvider;
            _mediaFileService = mediaFileService;
            _seriesService = seriesService;
            _configService = configService;
            _eventAggregator = eventAggregator;
            _logger = logger;
        }

        public void DeleteComicFile(Series series, ComicFile comicFile)
        {
            var fullPath = comicFile.Path;
            var rootFolder = _diskProvider.GetParentFolder(series.Path);

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

            if (_diskProvider.FolderExists(series.Path))
            {
                var subfolder = _diskProvider.GetParentFolder(series.Path).GetRelativePath(_diskProvider.GetParentFolder(fullPath));
                DeleteComicFile(comicFile, subfolder);
            }
            else
            {
                // delete from db even if the series folder is missing
                _mediaFileService.Delete(comicFile, DeleteMediaFileReason.Manual);
            }
        }

        public void DeleteComicFile(ComicFile comicFile, string subfolder = "")
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
                var series = message.Series;
                var allSeries = _seriesService.AllSeriesPaths();

                foreach (var s in allSeries)
                {
                    if (s.Key == series.Id)
                    {
                        continue;
                    }

                    if (series.Path.IsParentPath(s.Value))
                    {
                        _logger.Error("Series path: '{0}' is a parent of another series, not deleting files.", series.Path);
                        return;
                    }

                    if (series.Path.PathEquals(s.Value))
                    {
                        _logger.Error("Series path: '{0}' is the same as another series, not deleting files.", series.Path);
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
                var series = message.ComicFile.Series.Value;
                var issueFolder = message.ComicFile.Path.GetParentPath();

                if (_diskProvider.GetFiles(series.Path, true).Empty())
                {
                    _diskProvider.DeleteFolder(series.Path, true);
                }
                else if (_diskProvider.GetFiles(issueFolder, true).Empty())
                {
                    _diskProvider.RemoveEmptySubfolders(issueFolder);
                }
            }
        }
    }
}
