using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.History;

namespace NzbDrone.Core.Download.TrackedDownloads
{
    public interface ITrackedDownloadAlreadyImported
    {
        bool IsImported(TrackedDownload trackedDownload, List<EntityHistory> historyItems);
    }

    public class TrackedDownloadAlreadyImported : ITrackedDownloadAlreadyImported
    {
        private readonly Logger _logger;

        public TrackedDownloadAlreadyImported(Logger logger)
        {
            _logger = logger;
        }

        public bool IsImported(TrackedDownload trackedDownload, List<EntityHistory> historyItems)
        {
            _logger.Trace("Checking if all issues for '{0}' have been imported", trackedDownload.DownloadItem.Title);

            if (historyItems.Empty())
            {
                _logger.Trace("No history for {0}", trackedDownload.DownloadItem.Title);
                return false;
            }

            if (trackedDownload.RemoteBook == null || trackedDownload.RemoteBook.Books == null)
            {
                return true;
            }

            var allBooksImportedInHistory = trackedDownload.RemoteBook.Books.All(issue =>
            {
                var lastHistoryItem = historyItems.FirstOrDefault(h => h.IssueId == issue.Id);

                if (lastHistoryItem == null)
                {
                    _logger.Trace($"No history for issue: {issue}");
                    return false;
                }

                _logger.Trace($"Last event for issue: {issue} is: {lastHistoryItem.EventType}");

                return lastHistoryItem.EventType == EntityHistoryEventType.ComicFileImported;
            });

            _logger.Trace("All issues for '{0}' have been imported: {1}", trackedDownload.DownloadItem.Title, allBooksImportedInHistory);

            return allBooksImportedInHistory;
        }
    }
}
