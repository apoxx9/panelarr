using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Books.Events;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.History
{
    public interface IHistoryService
    {
        PagingSpec<EntityHistory> Paged(PagingSpec<EntityHistory> pagingSpec);
        EntityHistory MostRecentForBook(int bookId);
        EntityHistory MostRecentForDownloadId(string downloadId);
        EntityHistory Get(int historyId);
        List<EntityHistory> GetBySeries(int authorId, EntityHistoryEventType? eventType);
        List<EntityHistory> GetByBook(int bookId, EntityHistoryEventType? eventType);
        List<EntityHistory> Find(string downloadId, EntityHistoryEventType eventType);
        List<EntityHistory> FindByDownloadId(string downloadId);
        string FindDownloadId(TrackImportedEvent trackedDownload);
        List<EntityHistory> Since(DateTime date, EntityHistoryEventType? eventType);
        void UpdateMany(IList<EntityHistory> items);
    }

    public class HistoryService : IHistoryService,
                                  IHandle<IssueGrabbedEvent>,
                                  IHandle<IssueImportIncompleteEvent>,
                                  IHandle<TrackImportedEvent>,
                                  IHandle<DownloadFailedEvent>,
                                  IHandle<ComicFileDeletedEvent>,
                                  IHandle<ComicFileRenamedEvent>,
                                  IHandle<ComicFileRetaggedEvent>,
                                  IHandle<SeriesDeletedEvent>,
                                  IHandle<DownloadIgnoredEvent>
    {
        private readonly IHistoryRepository _historyRepository;
        private readonly Logger _logger;

        public HistoryService(IHistoryRepository historyRepository, Logger logger)
        {
            _historyRepository = historyRepository;
            _logger = logger;
        }

        public PagingSpec<EntityHistory> Paged(PagingSpec<EntityHistory> pagingSpec)
        {
            return _historyRepository.GetPaged(pagingSpec);
        }

        public EntityHistory MostRecentForBook(int bookId)
        {
            return _historyRepository.MostRecentForBook(bookId);
        }

        public EntityHistory MostRecentForDownloadId(string downloadId)
        {
            return _historyRepository.MostRecentForDownloadId(downloadId);
        }

        public EntityHistory Get(int historyId)
        {
            return _historyRepository.Get(historyId);
        }

        public List<EntityHistory> GetBySeries(int authorId, EntityHistoryEventType? eventType)
        {
            return _historyRepository.GetBySeries(authorId, eventType);
        }

        public List<EntityHistory> GetByBook(int bookId, EntityHistoryEventType? eventType)
        {
            return _historyRepository.GetByBook(bookId, eventType);
        }

        public List<EntityHistory> Find(string downloadId, EntityHistoryEventType eventType)
        {
            return _historyRepository.FindByDownloadId(downloadId).Where(c => c.EventType == eventType).ToList();
        }

        public List<EntityHistory> FindByDownloadId(string downloadId)
        {
            return _historyRepository.FindByDownloadId(downloadId);
        }

        public string FindDownloadId(TrackImportedEvent trackedDownload)
        {
            _logger.Debug("Trying to find downloadId for {0} from history", trackedDownload.ImportedBook.Path);

            var bookIds = new List<int> { trackedDownload.BookInfo.Issue.Id };
            var allHistory = _historyRepository.FindDownloadHistory(trackedDownload.BookInfo.Series.Id, trackedDownload.ImportedBook.Quality);

            //Find download related items for these episodes
            var booksHistory = allHistory.Where(h => bookIds.Contains(h.IssueId)).ToList();

            var processedDownloadId = booksHistory
                .Where(c => c.EventType != EntityHistoryEventType.Grabbed && c.DownloadId != null)
                .Select(c => c.DownloadId);

            var stillDownloading = booksHistory.Where(c => c.EventType == EntityHistoryEventType.Grabbed && !processedDownloadId.Contains(c.DownloadId)).ToList();

            string downloadId = null;

            if (stillDownloading.Any())
            {
                var matchingHistory = stillDownloading.Where(c => c.IssueId == trackedDownload.BookInfo.Issue.Id).ToList();

                if (matchingHistory.Count != 1)
                {
                    return null;
                }

                var newDownloadId = matchingHistory.Single().DownloadId;

                if (downloadId == null || downloadId == newDownloadId)
                {
                    downloadId = newDownloadId;
                }
                else
                {
                    return null;
                }
            }

            return downloadId;
        }

        public void Handle(IssueGrabbedEvent message)
        {
            foreach (var issue in message.Issue.Books)
            {
                var history = new EntityHistory
                {
                    EventType = EntityHistoryEventType.Grabbed,
                    Date = DateTime.UtcNow,
                    Quality = message.Issue.ParsedIssueInfo.Quality,
                    SourceTitle = message.Issue.Release.Title,
                    SeriesId = issue.SeriesId,
                    IssueId = issue.Id,
                    DownloadId = message.DownloadId
                };

                history.Data.Add("Indexer", message.Issue.Release.Indexer);
                history.Data.Add("NzbInfoUrl", message.Issue.Release.InfoUrl);
                history.Data.Add("ReleaseGroup", message.Issue.ParsedIssueInfo.ReleaseGroup);
                history.Data.Add("Age", message.Issue.Release.Age.ToString());
                history.Data.Add("AgeHours", message.Issue.Release.AgeHours.ToString());
                history.Data.Add("AgeMinutes", message.Issue.Release.AgeMinutes.ToString());
                history.Data.Add("PublishedDate", message.Issue.Release.PublishDate.ToString("s") + "Z");
                history.Data.Add("DownloadClient", message.DownloadClient);
                history.Data.Add("DownloadClientName", message.DownloadClientName);
                history.Data.Add("Size", message.Issue.Release.Size.ToString());
                history.Data.Add("DownloadUrl", message.Issue.Release.DownloadUrl);
                history.Data.Add("Guid", message.Issue.Release.Guid);
                history.Data.Add("Protocol", ((int)message.Issue.Release.DownloadProtocol).ToString());
                history.Data.Add("DownloadForced", (!message.Issue.DownloadAllowed).ToString());
                history.Data.Add("CustomFormatScore", message.Issue.CustomFormatScore.ToString());
                history.Data.Add("ReleaseSource", message.Issue.ReleaseSource.ToString());
                history.Data.Add("IndexerFlags", message.Issue.Release.IndexerFlags.ToString());

                if (!message.Issue.ParsedIssueInfo.ReleaseHash.IsNullOrWhiteSpace())
                {
                    history.Data.Add("ReleaseHash", message.Issue.ParsedIssueInfo.ReleaseHash);
                }

                if (message.Issue.Release is TorrentInfo torrentRelease)
                {
                    history.Data.Add("TorrentInfoHash", torrentRelease.InfoHash);
                }

                _historyRepository.Insert(history);
            }
        }

        public void Handle(IssueImportIncompleteEvent message)
        {
            if (message.TrackedDownload.RemoteBook == null)
            {
                return;
            }

            foreach (var issue in message.TrackedDownload.RemoteBook.Books)
            {
                var history = new EntityHistory
                {
                    EventType = EntityHistoryEventType.IssueImportIncomplete,
                    Date = DateTime.UtcNow,
                    Quality = message.TrackedDownload.RemoteBook.ParsedIssueInfo?.Quality ?? new QualityModel(),
                    SourceTitle = message.TrackedDownload.DownloadItem.Title,
                    SeriesId = issue.SeriesId,
                    IssueId = issue.Id,
                    DownloadId = message.TrackedDownload.DownloadItem.DownloadId
                };

                history.Data.Add("StatusMessages", message.TrackedDownload.StatusMessages.ToJson());
                history.Data.Add("ReleaseGroup", message.TrackedDownload?.RemoteBook?.ParsedIssueInfo?.ReleaseGroup);
                history.Data.Add("IndexerFlags", message.TrackedDownload?.RemoteBook?.Release?.IndexerFlags.ToString());

                _historyRepository.Insert(history);
            }
        }

        public void Handle(TrackImportedEvent message)
        {
            if (!message.NewDownload)
            {
                return;
            }

            var downloadId = message.DownloadId;

            if (downloadId.IsNullOrWhiteSpace())
            {
                downloadId = FindDownloadId(message);
            }

            var history = new EntityHistory
            {
                EventType = EntityHistoryEventType.ComicFileImported,
                Date = DateTime.UtcNow,
                Quality = message.BookInfo.Quality,
                SourceTitle = message.ImportedBook.SceneName ?? Path.GetFileNameWithoutExtension(message.BookInfo.Path),
                SeriesId = message.BookInfo.Series.Id,
                IssueId = message.BookInfo.Issue.Id,
                DownloadId = downloadId
            };

            history.Data.Add("FileId", message.ImportedBook.Id.ToString());
            history.Data.Add("DroppedPath", message.BookInfo.Path);
            history.Data.Add("ImportedPath", message.ImportedBook.Path);
            history.Data.Add("DownloadClient", message.DownloadClientInfo?.Type);
            history.Data.Add("DownloadClientName", message.DownloadClientInfo?.Name);
            history.Data.Add("ReleaseGroup", message.BookInfo.ReleaseGroup);
            history.Data.Add("Size", message.BookInfo.Size.ToString());
            history.Data.Add("IndexerFlags", message.BookInfo.IndexerFlags.ToString());

            _historyRepository.Insert(history);
        }

        public void Handle(DownloadFailedEvent message)
        {
            foreach (var bookId in message.IssueIds)
            {
                var history = new EntityHistory
                {
                    EventType = EntityHistoryEventType.DownloadFailed,
                    Date = DateTime.UtcNow,
                    Quality = message.Quality,
                    SourceTitle = message.SourceTitle,
                    SeriesId = message.SeriesId,
                    IssueId = bookId,
                    DownloadId = message.DownloadId
                };

                history.Data.Add("DownloadClient", message.DownloadClient);
                history.Data.Add("DownloadClientName", message.TrackedDownload?.DownloadItem.DownloadClientInfo.Name);
                history.Data.Add("Message", message.Message);
                history.Data.Add("ReleaseGroup", message.TrackedDownload?.RemoteBook?.ParsedIssueInfo?.ReleaseGroup ?? message.Data.GetValueOrDefault(EntityHistory.RELEASE_GROUP));
                history.Data.Add("Size", message.TrackedDownload?.DownloadItem.TotalSize.ToString() ?? message.Data.GetValueOrDefault(EntityHistory.SIZE));
                history.Data.Add("Indexer", message.TrackedDownload?.RemoteBook?.Release?.Indexer ?? message.Data.GetValueOrDefault(EntityHistory.INDEXER));

                _historyRepository.Insert(history);
            }
        }

        public void Handle(ComicFileDeletedEvent message)
        {
            if (message.Reason == DeleteMediaFileReason.NoLinkedEpisodes)
            {
                _logger.Debug("Removing issue file from DB as part of cleanup routine, not creating history event.");
                return;
            }
            else if (message.Reason == DeleteMediaFileReason.ManualOverride)
            {
                _logger.Debug("Removing issue file from DB as part of manual override of existing file, not creating history event.");
                return;
            }

            var history = new EntityHistory
            {
                EventType = EntityHistoryEventType.ComicFileDeleted,
                Date = DateTime.UtcNow,
                Quality = message.ComicFile.Quality,
                SourceTitle = message.ComicFile.Path,
                SeriesId = message.ComicFile.Series.Value.Id,
                IssueId = message.ComicFile.IssueId
            };

            history.Data.Add("Reason", message.Reason.ToString());
            history.Data.Add("ReleaseGroup", message.ComicFile.ReleaseGroup);
            history.Data.Add("IndexerFlags", message.ComicFile.IndexerFlags.ToString());

            _historyRepository.Insert(history);
        }

        public void Handle(ComicFileRenamedEvent message)
        {
            var sourcePath = message.OriginalPath;
            var path = message.ComicFile.Path;

            var history = new EntityHistory
            {
                EventType = EntityHistoryEventType.ComicFileRenamed,
                Date = DateTime.UtcNow,
                Quality = message.ComicFile.Quality,
                SourceTitle = message.OriginalPath,
                SeriesId = message.ComicFile.Series.Value.Id,
                IssueId = message.ComicFile.IssueId
            };

            history.Data.Add("SourcePath", sourcePath);
            history.Data.Add("Path", path);
            history.Data.Add("ReleaseGroup", message.ComicFile.ReleaseGroup);
            history.Data.Add("Size", message.ComicFile.Size.ToString());
            history.Data.Add("IndexerFlags", message.ComicFile.IndexerFlags.ToString());

            _historyRepository.Insert(history);
        }

        public void Handle(ComicFileRetaggedEvent message)
        {
            var path = message.ComicFile.Path;

            var history = new EntityHistory
            {
                EventType = EntityHistoryEventType.ComicFileRetagged,
                Date = DateTime.UtcNow,
                Quality = message.ComicFile.Quality,
                SourceTitle = path,
                SeriesId = message.ComicFile.Series.Value.Id,
                IssueId = message.ComicFile.IssueId
            };

            history.Data.Add("TagsScrubbed", message.Scrubbed.ToString());
            history.Data.Add("Diff", message.Diff.Select(x => new
            {
                Field = x.Key,
                OldValue = x.Value.Item1,
                NewValue = x.Value.Item2
            }).ToJson());

            _historyRepository.Insert(history);
        }

        public void Handle(SeriesDeletedEvent message)
        {
            _historyRepository.DeleteForSeries(message.Series.Id);
        }

        public void Handle(DownloadIgnoredEvent message)
        {
            var historyToAdd = new List<EntityHistory>();
            foreach (var bookId in message.IssueIds)
            {
                var history = new EntityHistory
                {
                    EventType = EntityHistoryEventType.DownloadIgnored,
                    Date = DateTime.UtcNow,
                    Quality = message.Quality,
                    SourceTitle = message.SourceTitle,
                    SeriesId = message.SeriesId,
                    IssueId = bookId,
                    DownloadId = message.DownloadId
                };

                history.Data.Add("DownloadClient", message.DownloadClientInfo.Name);
                history.Data.Add("Message", message.Message);
                history.Data.Add("ReleaseGroup", message.TrackedDownload?.RemoteBook?.ParsedIssueInfo?.ReleaseGroup);
                history.Data.Add("Size", message.TrackedDownload?.DownloadItem.TotalSize.ToString());
                history.Data.Add("Indexer", message.TrackedDownload?.RemoteBook?.Release?.Indexer);

                historyToAdd.Add(history);
            }

            _historyRepository.InsertMany(historyToAdd);
        }

        public List<EntityHistory> Since(DateTime date, EntityHistoryEventType? eventType)
        {
            return _historyRepository.Since(date, eventType);
        }

        public void UpdateMany(IList<EntityHistory> items)
        {
            _historyRepository.UpdateMany(items);
        }
    }
}
