using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Download;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Update.History.Events;

namespace NzbDrone.Core.Notifications
{
    public class NotificationService
        : IHandle<IssueGrabbedEvent>,
          IHandle<IssueImportedEvent>,
          IHandle<SeriesRenamedEvent>,
          IHandle<SeriesAddedEvent>,
          IHandle<SeriesDeletedEvent>,
          IHandle<IssueDeletedEvent>,
          IHandle<ComicFileDeletedEvent>,
          IHandle<HealthCheckFailedEvent>,
          IHandle<DownloadFailedEvent>,
          IHandle<IssueImportIncompleteEvent>,
          IHandle<ComicFileRetaggedEvent>,
          IHandleAsync<DeleteCompletedEvent>,
          IHandle<UpdateInstalledEvent>
    {
        private readonly INotificationFactory _notificationFactory;
        private readonly INotificationStatusService _notificationStatusService;
        private readonly Logger _logger;

        public NotificationService(INotificationFactory notificationFactory, INotificationStatusService notificationStatusService, Logger logger)
        {
            _notificationFactory = notificationFactory;
            _notificationStatusService = notificationStatusService;
            _logger = logger;
        }

        private string GetMessage(Series series, List<Issue> issues, QualityModel quality)
        {
            var qualityString = quality.Quality.ToString();

            if (quality.Revision.Version > 1)
            {
                qualityString += " Proper";
            }

            // Comic grab format: "Grabbed: {Series} #{IssueNumber} - {Title}"
            var issueDescriptions = issues.Select(e =>
            {
                var issueNum = e.IssueNumber.ToString("0.##");
                return string.IsNullOrWhiteSpace(e.Title)
                    ? $"#{issueNum}"
                    : $"#{issueNum} - {e.Title}";
            });

            return string.Format("Grabbed: {0} {1} [{2}]",
                                    series.Name,
                                    string.Join(" + ", issueDescriptions),
                                    qualityString);
        }

        private string GetIssueDownloadMessage(Series series, Issue issue, List<ComicFile> tracks)
        {
            // Comic import format: "Imported: {Series} #{IssueNumber} - {Title} [{Quality}]"
            var quality = tracks.FirstOrDefault()?.Quality?.Quality?.ToString() ?? "Unknown";
            var issueNum = issue.IssueNumber.ToString("0.##");
            var title = string.IsNullOrWhiteSpace(issue.Title)
                ? string.Empty
                : $" - {issue.Title}";

            return string.Format("Imported: {0} #{1}{2} [{3}]",
                series.Name,
                issueNum,
                title,
                quality);
        }

        private string GetIssueIncompleteImportMessage(string source)
        {
            return string.Format("Panelarr failed to Import all files for {0}",
                source);
        }

        private string FormatMissing(object value)
        {
            var text = value?.ToString();
            return text.IsNullOrWhiteSpace() ? "<missing>" : text;
        }

        private string GetTrackRetagMessage(Series series, ComicFile comicFile, Dictionary<string, Tuple<string, string>> diff)
        {
            return string.Format("{0}:\n{1}",
                                 comicFile.Path,
                                 string.Join("\n", diff.Select(x => $"{x.Key}: {FormatMissing(x.Value.Item1)} → {FormatMissing(x.Value.Item2)}")));
        }

        private bool ShouldHandleSeries(ProviderDefinition definition, Series series)
        {
            if (definition.Tags.Empty())
            {
                _logger.Debug("No tags set for this notification.");
                return true;
            }

            if (definition.Tags.Intersect(series.Tags).Any())
            {
                _logger.Debug("Notification and series have one or more intersecting tags.");
                return true;
            }

            //TODO: this message could be more clear
            _logger.Debug("{0} does not have any intersecting tags with {1}. Notification will not be sent.", definition.Name, series.Name);
            return false;
        }

        private bool ShouldHandleHealthFailure(HealthCheck.HealthCheck healthCheck, bool includeWarnings)
        {
            if (healthCheck.Type == HealthCheckResult.Error)
            {
                return true;
            }

            if (healthCheck.Type == HealthCheckResult.Warning && includeWarnings)
            {
                return true;
            }

            return false;
        }

        public void Handle(IssueGrabbedEvent message)
        {
            var grabMessage = new GrabMessage
            {
                Message = GetMessage(message.Issue.Series, message.Issue.Issues, message.Issue.ParsedIssueInfo.Quality),
                Series = message.Issue.Series,
                Quality = message.Issue.ParsedIssueInfo.Quality,
                RemoteIssue = message.Issue,
                DownloadClientName = message.DownloadClientName,
                DownloadClientType = message.DownloadClient,
                DownloadId = message.DownloadId
            };

            foreach (var notification in _notificationFactory.OnGrabEnabled())
            {
                try
                {
                    if (!ShouldHandleSeries(notification.Definition, message.Issue.Series))
                    {
                        continue;
                    }

                    notification.OnGrab(grabMessage);
                    _notificationStatusService.RecordSuccess(notification.Definition.Id);
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Error(ex, "Unable to send OnGrab notification to {0}", notification.Definition.Name);
                }
            }
        }

        public void Handle(IssueImportedEvent message)
        {
            if (!message.NewDownload)
            {
                return;
            }

            var isUpgrade = message.OldFiles != null && message.OldFiles.Count > 0;
            string downloadMsg;

            if (isUpgrade)
            {
                var oldQuality = message.OldFiles.First()?.Quality?.Quality?.ToString() ?? "Unknown";
                var newQuality = message.ImportedIssues.FirstOrDefault()?.Quality?.Quality?.ToString() ?? "Unknown";
                var issueNum = message.Issue.IssueNumber.ToString("0.##");
                var title = string.IsNullOrWhiteSpace(message.Issue.Title)
                    ? string.Empty
                    : $" - {message.Issue.Title}";
                downloadMsg = string.Format(
                    "Upgraded: {0} #{1}{2} from {3} to {4}",
                    message.Series.Name,
                    issueNum,
                    title,
                    oldQuality,
                    newQuality);
            }
            else
            {
                downloadMsg = GetIssueDownloadMessage(message.Series, message.Issue, message.ImportedIssues);
            }

            var downloadMessage = new IssueDownloadMessage
            {
                Message = downloadMsg,
                Series = message.Series,
                Issue = message.Issue,
                DownloadClientInfo = message.DownloadClientInfo,
                DownloadId = message.DownloadId,
                ComicFiles = message.ImportedIssues,
                OldFiles = message.OldFiles,
            };

            foreach (var notification in _notificationFactory.OnReleaseImportEnabled())
            {
                try
                {
                    if (ShouldHandleSeries(notification.Definition, message.Series))
                    {
                        if (downloadMessage.OldFiles.Empty() || ((NotificationDefinition)notification.Definition).OnUpgrade)
                        {
                            notification.OnReleaseImport(downloadMessage);
                            _notificationStatusService.RecordSuccess(notification.Definition.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnReleaseImport notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(SeriesRenamedEvent message)
        {
            foreach (var notification in _notificationFactory.OnRenameEnabled())
            {
                try
                {
                    if (ShouldHandleSeries(notification.Definition, message.Series))
                    {
                        notification.OnRename(message.Series, message.RenamedFiles);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnRename notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(SeriesAddedEvent message)
        {
            foreach (var notification in _notificationFactory.OnSeriesAddedEnabled())
            {
                try
                {
                    if (ShouldHandleSeries(notification.Definition, message.Series))
                    {
                        notification.OnSeriesAdded(message.Series);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnSeriesAdded notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(SeriesDeletedEvent message)
        {
            var deleteMessage = new SeriesDeleteMessage(message.Series, message.DeleteFiles);

            foreach (var notification in _notificationFactory.OnSeriesDeleteEnabled())
            {
                try
                {
                    if (ShouldHandleSeries(notification.Definition, deleteMessage.Series))
                    {
                        notification.OnSeriesDelete(deleteMessage);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnSeriesDelete notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(IssueDeletedEvent message)
        {
            var deleteMessage = new IssueDeleteMessage(message.Issue, message.DeleteFiles);

            foreach (var notification in _notificationFactory.OnIssueDeleteEnabled())
            {
                try
                {
                    if (ShouldHandleSeries(notification.Definition, deleteMessage.Issue.Series))
                    {
                        notification.OnIssueDelete(deleteMessage);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnIssueDelete notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(ComicFileDeletedEvent message)
        {
            var deleteMessage = new ComicFileDeleteMessage();

            var issue = new List<Issue> { message.ComicFile.Issue?.Value };

            deleteMessage.Message = GetMessage(message.ComicFile.Series, issue, message.ComicFile.Quality);
            deleteMessage.ComicFile = message.ComicFile;
            deleteMessage.Issue = message.ComicFile.Issue?.Value;
            deleteMessage.Reason = message.Reason;

            foreach (var notification in _notificationFactory.OnComicFileDeleteEnabled())
            {
                try
                {
                    if (message.Reason != MediaFiles.DeleteMediaFileReason.Upgrade || ((NotificationDefinition)notification.Definition).OnComicFileDeleteForUpgrade)
                    {
                        if (ShouldHandleSeries(notification.Definition, message.ComicFile.Series))
                        {
                            notification.OnComicFileDelete(deleteMessage);
                            _notificationStatusService.RecordSuccess(notification.Definition.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnComicFileDelete notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(HealthCheckFailedEvent message)
        {
            // Don't send health check notifications during the start up grace period,
            // once that duration expires they they'll be retested and fired off if necessary.
            if (message.IsInStartupGracePeriod)
            {
                return;
            }

            foreach (var notification in _notificationFactory.OnHealthIssueEnabled())
            {
                try
                {
                    if (ShouldHandleHealthFailure(message.HealthCheck, ((NotificationDefinition)notification.Definition).IncludeHealthWarnings))
                    {
                        notification.OnHealthIssue(message.HealthCheck);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnHealthIssue notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(DownloadFailedEvent message)
        {
            var downloadFailedMessage = new DownloadFailedMessage
            {
                DownloadId = message.DownloadId,
                DownloadClient = message.DownloadClient,
                Quality = message.Quality,
                SourceTitle = message.SourceTitle,
                Message = message.Message
            };

            foreach (var notification in _notificationFactory.OnDownloadFailureEnabled())
            {
                try
                {
                    if (ShouldHandleSeries(notification.Definition, message.TrackedDownload.RemoteIssue.Series))
                    {
                        notification.OnDownloadFailure(downloadFailedMessage);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnDownloadFailure notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(IssueImportIncompleteEvent message)
        {
            // TODO: Build out this message so that we can pass on what failed and what was successful
            var downloadMessage = new IssueDownloadMessage
            {
                Message = GetIssueIncompleteImportMessage(message.TrackedDownload.DownloadItem.Title)
            };

            foreach (var notification in _notificationFactory.OnImportFailureEnabled())
            {
                try
                {
                    if (ShouldHandleSeries(notification.Definition, message.TrackedDownload.RemoteIssue.Series))
                    {
                        notification.OnImportFailure(downloadMessage);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnImportFailure notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(ComicFileRetaggedEvent message)
        {
            var retagMessage = new IssueRetagMessage
            {
                Message = GetTrackRetagMessage(message.Series, message.ComicFile, message.Diff),
                Series = message.Series,
                Issue = message.ComicFile.Issue?.Value,
                ComicFile = message.ComicFile,
                Diff = message.Diff,
                Scrubbed = message.Scrubbed
            };

            foreach (var notification in _notificationFactory.OnIssueRetagEnabled())
            {
                try
                {
                    if (ShouldHandleSeries(notification.Definition, message.Series))
                    {
                        notification.OnIssueRetag(retagMessage);
                        _notificationStatusService.RecordSuccess(notification.Definition.Id);
                    }
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnIssueRetag notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(UpdateInstalledEvent message)
        {
            var updateMessage = new ApplicationUpdateMessage();
            updateMessage.Message = $"Panelarr updated from {message.PreviousVerison.ToString()} to {message.NewVersion.ToString()}";
            updateMessage.PreviousVersion = message.PreviousVerison;
            updateMessage.NewVersion = message.NewVersion;

            foreach (var notification in _notificationFactory.OnApplicationUpdateEnabled())
            {
                try
                {
                    notification.OnApplicationUpdate(updateMessage);
                    _notificationStatusService.RecordSuccess(notification.Definition.Id);
                }
                catch (Exception ex)
                {
                    _notificationStatusService.RecordFailure(notification.Definition.Id);
                    _logger.Warn(ex, "Unable to send OnApplicationUpdate notification to: " + notification.Definition.Name);
                }
            }
        }

        public void HandleAsync(DeleteCompletedEvent message)
        {
            ProcessQueue();
        }

        private void ProcessQueue()
        {
            foreach (var notification in _notificationFactory.GetAvailableProviders())
            {
                try
                {
                    notification.ProcessQueue();
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to process notification queue for " + notification.Definition.Name);
                }
            }
        }
    }
}
