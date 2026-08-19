using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Download.History;
using NzbDrone.Core.History;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.Download.TrackedDownloads
{
    public interface ITrackedDownloadService
    {
        TrackedDownload Find(string downloadId);
        void StopTracking(string downloadId);
        void StopTracking(List<string> downloadIds);
        TrackedDownload TrackDownload(DownloadClientDefinition downloadClient, DownloadClientItem downloadItem);
        List<TrackedDownload> GetTrackedDownloads();
        void UpdateTrackable(List<TrackedDownload> trackedDownloads);
    }

    public class TrackedDownloadService : ITrackedDownloadService,
                                          IHandle<IssueGrabbedEvent>,
                                          IHandle<IssueInfoRefreshedEvent>,
                                          IHandle<SeriesDeletedEvent>
    {
        private readonly IParsingService _parsingService;
        private readonly IHistoryService _historyService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDownloadHistoryService _downloadHistoryService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly Logger _logger;
        private readonly ICached<TrackedDownload> _cache;

        public TrackedDownloadService(IParsingService parsingService,
                                      ICacheManager cacheManager,
                                      IHistoryService historyService,
                                      IEventAggregator eventAggregator,
                                      IDownloadHistoryService downloadHistoryService,
                                      ICustomFormatCalculationService formatCalculator,
                                      Logger logger)
        {
            _parsingService = parsingService;
            _historyService = historyService;
            _cache = cacheManager.GetCache<TrackedDownload>(GetType());
            _formatCalculator = formatCalculator;
            _eventAggregator = eventAggregator;
            _downloadHistoryService = downloadHistoryService;
            _cache = cacheManager.GetCache<TrackedDownload>(GetType());
            _logger = logger;
        }

        public TrackedDownload Find(string downloadId)
        {
            return _cache.Find(downloadId);
        }

        public void UpdateIssueCache(int issueId)
        {
            var updateCacheItems = _cache.Values.Where(x => x.RemoteIssue != null && x.RemoteIssue.Issues.Any(a => a.Id == issueId)).ToList();

            if (updateCacheItems.Any())
            {
                foreach (var item in updateCacheItems)
                {
                    var parsedIssueInfo = Parser.Parser.ParseIssueTitle(item.DownloadItem.Title);
                    item.RemoteIssue = null;

                    if (parsedIssueInfo != null)
                    {
                        item.RemoteIssue = _parsingService.Map(parsedIssueInfo);
                    }
                }

                _eventAggregator.PublishEvent(new TrackedDownloadRefreshedEvent(GetTrackedDownloads()));
            }
        }

        public void StopTracking(string downloadId)
        {
            var trackedDownload = _cache.Find(downloadId);

            _cache.Remove(downloadId);
            _eventAggregator.PublishEvent(new TrackedDownloadsRemovedEvent(new List<TrackedDownload> { trackedDownload }));
        }

        public void StopTracking(List<string> downloadIds)
        {
            var trackedDownloads = new List<TrackedDownload>();

            foreach (var downloadId in downloadIds)
            {
                var trackedDownload = _cache.Find(downloadId);
                _cache.Remove(downloadId);
                trackedDownloads.Add(trackedDownload);
            }

            _eventAggregator.PublishEvent(new TrackedDownloadsRemovedEvent(trackedDownloads));
        }

        public TrackedDownload TrackDownload(DownloadClientDefinition downloadClient, DownloadClientItem downloadItem)
        {
            var existingItem = Find(downloadItem.DownloadId);

            if (existingItem != null && existingItem.State != TrackedDownloadState.Downloading)
            {
                LogItemChange(existingItem, existingItem.DownloadItem, downloadItem);

                existingItem.DownloadItem = downloadItem;
                existingItem.IsTrackable = true;

                return existingItem;
            }

            var trackedDownload = new TrackedDownload
            {
                DownloadClient = downloadClient.Id,
                DownloadItem = downloadItem,
                Protocol = downloadClient.Protocol,
                IsTrackable = true
            };

            try
            {
                var parsedIssueInfo = Parser.Parser.ParseIssueTitle(trackedDownload.DownloadItem.Title);
                var historyItems = _historyService.FindByDownloadId(downloadItem.DownloadId)
                    .OrderByDescending(h => h.Date)
                    .ToList();

                if (parsedIssueInfo != null)
                {
                    trackedDownload.RemoteIssue = _parsingService.Map(parsedIssueInfo);
                }

                var downloadHistory = _downloadHistoryService.GetLatestDownloadHistoryItem(downloadItem.DownloadId);

                if (downloadHistory != null)
                {
                    var state = GetStateFromHistory(downloadHistory.EventType);
                    trackedDownload.State = state;

                    if (downloadHistory.EventType == DownloadHistoryEventType.DownloadImportIncomplete)
                    {
                        var messages = Json.Deserialize<List<TrackedDownloadStatusMessage>>(downloadHistory.Data["statusMessages"]).ToArray();
                        trackedDownload.Warn(messages);
                    }
                }

                if (historyItems.Any())
                {
                    var firstHistoryItem = historyItems.First();
                    var grabbedEvent = historyItems.FirstOrDefault(v => v.EventType == EntityHistoryEventType.Grabbed);

                    trackedDownload.Indexer = grabbedEvent?.Data?.GetValueOrDefault("indexer");

                    // The grab record already names the exact series and issues
                    // the decision engine chose - that is authoritative. Only
                    // re-parse the title when the record has nothing: title
                    // re-parsing a collected-edition name without search
                    // criteria yields a series with no issue, and the download
                    // landed in the queue as unknown with nothing to import into.
                    var grabbedIssueIds = historyItems
                        .Where(v => v.EventType == EntityHistoryEventType.Grabbed && v.IssueId > 0)
                        .Select(h => h.IssueId)
                        .Distinct()
                        .ToList();

                    if (grabbedEvent != null && grabbedEvent.SeriesId > 0 && grabbedIssueIds.Any() &&
                        (trackedDownload.RemoteIssue?.Series == null || trackedDownload.RemoteIssue.Issues.Empty()))
                    {
                        var info = parsedIssueInfo ?? new ParsedIssueInfo
                        {
                            SeriesName = grabbedEvent.SourceTitle,
                            Quality = QualityParser.ParseQuality(grabbedEvent.SourceTitle)
                        };

                        trackedDownload.RemoteIssue = _parsingService.Map(info, grabbedEvent.SeriesId, grabbedIssueIds);
                    }

                    if (parsedIssueInfo == null ||
                        trackedDownload.RemoteIssue?.Series == null ||
                        trackedDownload.RemoteIssue.Issues.Empty())
                    {
                        // Try parsing the original source title and if that fails, try parsing it as a special
                        var historySeries = firstHistoryItem.Series;
                        var historyIssues = new List<Issue> { firstHistoryItem.Issue };

                        parsedIssueInfo = Parser.Parser.ParseIssueTitle(firstHistoryItem.SourceTitle);

                        if (parsedIssueInfo != null)
                        {
                            trackedDownload.RemoteIssue = _parsingService.Map(parsedIssueInfo,
                                firstHistoryItem.SeriesId,
                                historyItems.Where(v => v.EventType == EntityHistoryEventType.Grabbed).Select(h => h.IssueId)
                                    .Distinct());
                        }
                        else
                        {
                            parsedIssueInfo =
                                Parser.Parser.ParseIssueTitleWithSearchCriteria(firstHistoryItem.SourceTitle,
                                    historySeries,
                                    historyIssues);

                            if (parsedIssueInfo != null)
                            {
                                trackedDownload.RemoteIssue = _parsingService.Map(parsedIssueInfo,
                                    firstHistoryItem.SeriesId,
                                    historyItems.Where(v => v.EventType == EntityHistoryEventType.Grabbed).Select(h => h.IssueId)
                                        .Distinct());
                            }
                        }
                    }

                    if (trackedDownload.RemoteIssue != null &&
                        Enum.TryParse(grabbedEvent?.Data?.GetValueOrDefault("indexerFlags"), true, out IndexerFlags flags))
                    {
                        trackedDownload.RemoteIssue.Release ??= new ReleaseInfo();
                        trackedDownload.RemoteIssue.Release.IndexerFlags = flags;
                    }
                }

                // Calculate custom formats
                if (trackedDownload.RemoteIssue != null)
                {
                    trackedDownload.RemoteIssue.CustomFormats = _formatCalculator.ParseCustomFormat(trackedDownload.RemoteIssue, downloadItem.TotalSize);
                }

                // Track it so it can be displayed in the queue even though we can't determine which artist it is for
                if (trackedDownload.RemoteIssue == null)
                {
                    _logger.Trace("No Issue found for download '{0}'", trackedDownload.DownloadItem.Title);
                }
            }
            catch (Exception e)
            {
                _logger.Debug(e, "Failed to find issue for " + downloadItem.Title);
                return null;
            }

            LogItemChange(trackedDownload, existingItem?.DownloadItem, trackedDownload.DownloadItem);

            _cache.Set(trackedDownload.DownloadItem.DownloadId, trackedDownload);
            return trackedDownload;
        }

        public List<TrackedDownload> GetTrackedDownloads()
        {
            return _cache.Values.ToList();
        }

        public void UpdateTrackable(List<TrackedDownload> trackedDownloads)
        {
            var untrackable = GetTrackedDownloads().ExceptBy(t => t.DownloadItem.DownloadId, trackedDownloads, t => t.DownloadItem.DownloadId, StringComparer.CurrentCulture).ToList();

            foreach (var trackedDownload in untrackable)
            {
                trackedDownload.IsTrackable = false;
            }
        }

        private void LogItemChange(TrackedDownload trackedDownload, DownloadClientItem existingItem, DownloadClientItem downloadItem)
        {
            if (existingItem == null ||
                existingItem.Status != downloadItem.Status ||
                existingItem.CanBeRemoved != downloadItem.CanBeRemoved ||
                existingItem.CanMoveFiles != downloadItem.CanMoveFiles)
            {
                _logger.Debug("Tracking '{0}:{1}': ClientState={2}{3} PanelarrStage={4} Issue='{5}' OutputPath={6}.",
                    downloadItem.DownloadClientInfo.Name,
                    downloadItem.Title,
                    downloadItem.Status,
                    downloadItem.CanBeRemoved ? "" : downloadItem.CanMoveFiles ? " (busy)" : " (readonly)",
                    trackedDownload.State,
                    trackedDownload.RemoteIssue?.ParsedIssueInfo,
                    downloadItem.OutputPath);
            }
        }

        private void UpdateCachedItem(TrackedDownload trackedDownload)
        {
            var parsedIssueInfo = Parser.Parser.ParseIssueTitle(trackedDownload.DownloadItem.Title);

            trackedDownload.RemoteIssue = parsedIssueInfo == null ? null : _parsingService.Map(parsedIssueInfo, 0, new[] { 0 });
        }

        private static TrackedDownloadState GetStateFromHistory(DownloadHistoryEventType eventType)
        {
            switch (eventType)
            {
                case DownloadHistoryEventType.DownloadImportIncomplete:
                    return TrackedDownloadState.ImportFailed;
                case DownloadHistoryEventType.DownloadImported:
                    return TrackedDownloadState.Imported;
                case DownloadHistoryEventType.DownloadFailed:
                    return TrackedDownloadState.DownloadFailed;
                case DownloadHistoryEventType.DownloadIgnored:
                    return TrackedDownloadState.Ignored;
                default:
                    return TrackedDownloadState.Downloading;
            }
        }

        public void Handle(IssueGrabbedEvent message)
        {
            if (message.DownloadId.IsNullOrWhiteSpace())
            {
                return;
            }

            var existingItem = _cache.Find(message.DownloadId);

            if (existingItem == null || existingItem.State == TrackedDownloadState.Downloading)
            {
                return;
            }

            // A cached item in a terminal state (Ignored/Imported/Failed) would short-circuit
            // TrackDownload and hide the new grab until a restart. Evict it so the refresh
            // triggered by this grab rebuilds it from download history.
            _logger.Debug("Download '{0}' was re-grabbed while cached as {1}, removing from cache to allow re-tracking", existingItem.DownloadItem.Title, existingItem.State);
            _cache.Remove(message.DownloadId);
        }

        public void Handle(IssueInfoRefreshedEvent message)
        {
            var needsToUpdate = false;

            foreach (var issue in message.Removed)
            {
                var cachedItems = _cache.Values.Where(t =>
                                            t.RemoteIssue?.Issues != null &&
                                            t.RemoteIssue.Issues.Any(e => e.Id == issue.Id))
                                        .ToList();

                if (cachedItems.Any())
                {
                    needsToUpdate = true;
                }

                cachedItems.ForEach(UpdateCachedItem);
            }

            if (needsToUpdate)
            {
                _eventAggregator.PublishEvent(new TrackedDownloadRefreshedEvent(GetTrackedDownloads()));
            }
        }

        public void Handle(SeriesDeletedEvent message)
        {
            var cachedItems = _cache.Values.Where(t =>
                                        t.RemoteIssue?.Series != null &&
                                        t.RemoteIssue.Series.Id == message.Series.Id)
                                    .ToList();

            if (cachedItems.Any())
            {
                cachedItems.ForEach(UpdateCachedItem);

                _eventAggregator.PublishEvent(new TrackedDownloadRefreshedEvent(GetTrackedDownloads()));
            }
        }
    }
}
