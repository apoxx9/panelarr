using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.MediaFiles.IssueImport;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Download
{
    public interface ICompletedDownloadService
    {
        void Check(TrackedDownload trackedDownload);
        void Import(TrackedDownload trackedDownload);
        bool VerifyImport(TrackedDownload trackedDownload, List<ImportResult> importResults);
    }

    public class CompletedDownloadService : ICompletedDownloadService
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IHistoryService _historyService;
        private readonly IProvideImportItemService _provideImportItemService;
        private readonly IDownloadedIssuesImportService _downloadedTracksImportService;
        private readonly ITrackedDownloadAlreadyImported _trackedDownloadAlreadyImported;
        private readonly Logger _logger;

        public CompletedDownloadService(IEventAggregator eventAggregator,
                                        IHistoryService historyService,
                                        IProvideImportItemService provideImportItemService,
                                        IDownloadedIssuesImportService downloadedTracksImportService,
                                        ITrackedDownloadAlreadyImported trackedDownloadAlreadyImported,
                                        Logger logger)
        {
            _eventAggregator = eventAggregator;
            _historyService = historyService;
            _provideImportItemService = provideImportItemService;
            _downloadedTracksImportService = downloadedTracksImportService;
            _trackedDownloadAlreadyImported = trackedDownloadAlreadyImported;
            _logger = logger;
        }

        public void Check(TrackedDownload trackedDownload)
        {
            if (trackedDownload.DownloadItem.Status != DownloadItemStatus.Completed)
            {
                return;
            }

            SetImportItem(trackedDownload);

            // Only process tracked downloads that are still downloading
            if (trackedDownload.State != TrackedDownloadState.Downloading)
            {
                return;
            }

            var historyItem = _historyService.MostRecentForDownloadId(trackedDownload.DownloadItem.DownloadId);

            if (historyItem == null && trackedDownload.DownloadItem.Category.IsNullOrWhiteSpace())
            {
                trackedDownload.Warn("Download wasn't grabbed by Panelarr and not in a category, Skipping.");
                return;
            }

            if (!ValidatePath(trackedDownload))
            {
                return;
            }

            trackedDownload.State = TrackedDownloadState.ImportPending;
        }

        public void Import(TrackedDownload trackedDownload)
        {
            SetImportItem(trackedDownload);

            if (!ValidatePath(trackedDownload))
            {
                return;
            }

            trackedDownload.State = TrackedDownloadState.Importing;

            var outputPath = trackedDownload.ImportItem.OutputPath.FullPath;
            var importResults = _downloadedTracksImportService.ProcessPath(outputPath, ImportMode.Auto, trackedDownload.RemoteIssue?.Series, trackedDownload.DownloadItem);

            if (importResults.Empty())
            {
                trackedDownload.Warn("No files found are eligible for import in {0}", outputPath);
                trackedDownload.State = TrackedDownloadState.ImportPending;
                return;
            }

            if (VerifyImport(trackedDownload, importResults))
            {
                return;
            }

            trackedDownload.State = TrackedDownloadState.ImportPending;

            if (importResults.Any(c => c.Result != ImportResultType.Imported))
            {
                trackedDownload.State = TrackedDownloadState.ImportFailed;
                var statusMessages = importResults
                    .Where(v => v.Result != ImportResultType.Imported && v.ImportDecision.Item != null)
                    .Select(v => new TrackedDownloadStatusMessage(Path.GetFileName(v.ImportDecision.Item.Path), v.Errors))
                    .ToArray();

                trackedDownload.Warn(statusMessages);
                _eventAggregator.PublishEvent(new IssueImportIncompleteEvent(trackedDownload));
                return;
            }
        }

        public bool VerifyImport(TrackedDownload trackedDownload, List<ImportResult> importResults)
        {
            var allItemsImported = importResults.Where(c => c.Result == ImportResultType.Imported)
                                                   .Select(c => c.ImportDecision.Item.Issue)
                                                   .Count() >= Math.Max(1, trackedDownload.RemoteIssue?.Issues.Count ?? 1);

            if (allItemsImported)
            {
                _logger.Debug("All issues were imported for {0}", trackedDownload.DownloadItem.Title);
                trackedDownload.State = TrackedDownloadState.Imported;

                var importedSeriesId = importResults.Where(x => x.Result == ImportResultType.Imported)
                    .Select(c => c.ImportDecision.Item.Series.Id)
                    .MostCommon();
                _eventAggregator.PublishEvent(new DownloadCompletedEvent(trackedDownload, trackedDownload.RemoteIssue?.Series.Id ?? importedSeriesId));
                return true;
            }

            // Double check if all episodes were imported by checking the history if at least one
            // file was imported. This will allow the decision engine to reject already imported
            // episode files and still mark the download complete when all files are imported.

            // EDGE CASE: This process relies on EpisodeIds being consistent between executions, if a series is updated
            // and an episode is removed, but later comes back with a different ID then Sonarr will treat it as incomplete.
            // Since imports should be relatively fast and these types of data changes are infrequent this should be quite
            // safe, but commenting for future benefit.
            var atLeastOneEpisodeImported = importResults.Any(c => c.Result == ImportResultType.Imported);

            var historyItems = _historyService.FindByDownloadId(trackedDownload.DownloadItem.DownloadId)
                                              .OrderByDescending(h => h.Date)
                                              .ToList();

            var allEpisodesImportedInHistory = _trackedDownloadAlreadyImported.IsImported(trackedDownload, historyItems);

            if (allEpisodesImportedInHistory)
            {
                // Log different error messages depending on the circumstances, but treat both as fully imported, because that's the reality.
                // The second message shouldn't be logged in most cases, but continued reporting would indicate an ongoing issue.
                if (atLeastOneEpisodeImported)
                {
                    _logger.Debug("All issues were imported in history for {0}", trackedDownload.DownloadItem.Title);
                }
                else
                {
                    _logger.ForDebugEvent()
                           .Message("No issues were just imported, but all issues were previously imported, possible issue with download history.")
                           .Property("SeriesId", trackedDownload.RemoteIssue.Series.Id)
                           .Property("DownloadId", trackedDownload.DownloadItem.DownloadId)
                           .Property("Title", trackedDownload.DownloadItem.Title)
                           .Property("Path", trackedDownload.DownloadItem.OutputPath.ToString())
                           .WriteSentryWarn("DownloadHistoryIncomplete")
                           .Log();
                }

                trackedDownload.State = TrackedDownloadState.Imported;

                var importedSeriesId = historyItems.Where(x => x.EventType == EntityHistoryEventType.ComicFileImported)
                    .Select(x => x.SeriesId)
                    .MostCommon();
                _eventAggregator.PublishEvent(new DownloadCompletedEvent(trackedDownload, trackedDownload.RemoteIssue?.Series.Id ?? importedSeriesId));

                return true;
            }

            _logger.Debug("Not all issues have been imported for {0}", trackedDownload.DownloadItem.Title);
            return false;
        }

        private void SetImportItem(TrackedDownload trackedDownload)
        {
            trackedDownload.ImportItem = _provideImportItemService.ProvideImportItem(trackedDownload.DownloadItem, trackedDownload.ImportItem);
        }

        private bool ValidatePath(TrackedDownload trackedDownload)
        {
            var downloadItemOutputPath = trackedDownload.ImportItem.OutputPath;

            if (downloadItemOutputPath.IsEmpty)
            {
                trackedDownload.Warn("Download doesn't contain intermediate path, Skipping.");
                return false;
            }

            if ((OsInfo.IsWindows && !downloadItemOutputPath.IsWindowsPath) ||
                (OsInfo.IsNotWindows && !downloadItemOutputPath.IsUnixPath))
            {
                trackedDownload.Warn("[{0}] is not a valid local path. You may need a Remote Path Mapping.", downloadItemOutputPath);
                return false;
            }

            return true;
        }
    }
}
