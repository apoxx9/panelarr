using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.History;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.ComicInfo;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.Issues
{
    public interface IRefreshIssueService
    {
        bool RefreshIssueInfo(Issue issue, List<Issue> remoteIssues, Series remoteData, bool forceUpdateFileTags);
        bool RefreshIssueInfo(List<Issue> issues, List<Issue> remoteIssues, Series remoteData, bool forceIssueRefresh, bool forceUpdateFileTags, DateTime? lastUpdate);
    }

    public class RefreshIssueService : RefreshEntityServiceBase<Issue, object>,
        IRefreshIssueService,
        IExecute<RefreshIssueCommand>,
        IExecute<BulkRefreshIssueCommand>
    {
        private readonly IIssueService _issueService;
        private readonly ISeriesService _seriesService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IAddSeriesService _addSeriesService;
        private readonly IProvideSeriesInfo _seriesInfo;
        private readonly IProvideIssueInfo _issueInfoProvider;
        private readonly IMediaFileService _mediaFileService;
        private readonly IComicInfoEmbedService _comicInfoEmbedService;
        private readonly IHistoryService _historyService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ICheckIfIssueShouldBeRefreshed _checkIfIssueShouldBeRefreshed;
        private readonly IMapCoversToLocal _mediaCoverService;
        private readonly Logger _logger;

        public RefreshIssueService(IIssueService issueService,
                                  ISeriesService seriesService,
                                  IRootFolderService rootFolderService,
                                  IAddSeriesService addSeriesService,
                                  ISeriesMetadataService seriesMetadataService,
                                  IProvideSeriesInfo seriesInfo,
                                  IProvideIssueInfo issueInfoProvider,
                                  IMediaFileService mediaFileService,
                                  IComicInfoEmbedService comicInfoEmbedService,
                                  IHistoryService historyService,
                                  IEventAggregator eventAggregator,
                                  ICheckIfIssueShouldBeRefreshed checkIfIssueShouldBeRefreshed,
                                  IMapCoversToLocal mediaCoverService,
                                  Logger logger)
        : base(logger, seriesMetadataService)
        {
            _issueService = issueService;
            _seriesService = seriesService;
            _rootFolderService = rootFolderService;
            _addSeriesService = addSeriesService;
            _seriesInfo = seriesInfo;
            _issueInfoProvider = issueInfoProvider;
            _mediaFileService = mediaFileService;
            _comicInfoEmbedService = comicInfoEmbedService;
            _historyService = historyService;
            _eventAggregator = eventAggregator;
            _checkIfIssueShouldBeRefreshed = checkIfIssueShouldBeRefreshed;
            _mediaCoverService = mediaCoverService;
            _logger = logger;
        }

        private Series GetSkyhookData(Issue issue)
        {
            try
            {
                var tuple = _issueInfoProvider.GetIssueInfo(issue.ForeignIssueId);
                var series = _seriesInfo.GetSeriesInfo(tuple.Item1);
                var newbook = tuple.Item2;

                newbook.Series = series;
                newbook.SeriesMetadata = series.Metadata.Value;
                newbook.SeriesMetadataId = issue.SeriesMetadataId;
                newbook.SeriesMetadata.Value.Id = issue.SeriesMetadataId;

                series.Issues = new List<Issue> { newbook };
                return series;
            }
            catch (IssueNotFoundException)
            {
                _logger.Error($"Could not find issue with id {issue.ForeignIssueId}");
            }

            return null;
        }

        protected override RemoteData GetRemoteData(Issue local, List<Issue> remote, Series data)
        {
            var result = new RemoteData();

            var issue = remote.SingleOrDefault(x => x.ForeignIssueId == local.ForeignIssueId);

            if (issue == null && ShouldDelete(local))
            {
                return result;
            }

            if (issue == null)
            {
                data = GetSkyhookData(local);
                issue = data.Issues.Value.SingleOrDefault(x => x.ForeignIssueId == local.ForeignIssueId);
            }

            result.Entity = issue;
            if (result.Entity != null)
            {
                result.Entity.Id = local.Id;
            }

            return result;
        }

        protected override void EnsureNewParent(Issue local, Issue remote)
        {
            // Make sure the appropriate series exists (it could be that an issue changes parent)
            // The seriesMetadata entry will be in the db but make sure a corresponding series is too
            // so that the issue doesn't just disappear.

            // TODO filter by metadata id before hitting database
            _logger.Trace($"Ensuring parent series exists [{remote.SeriesMetadata.Value.ForeignSeriesId}]");

            var newSeries = _seriesService.FindById(remote.SeriesMetadata.Value.ForeignSeriesId);

            if (newSeries == null)
            {
                var oldSeries = local.Series.Value;
                var addSeries = new Series
                {
                    Metadata = remote.SeriesMetadata.Value,
                    QualityProfileId = oldSeries.QualityProfileId,
                    RootFolderPath = _rootFolderService.GetBestRootFolderPath(oldSeries.Path),
                    Monitored = oldSeries.Monitored,
                    Tags = oldSeries.Tags
                };
                _logger.Debug($"Adding missing parent series {addSeries}");
                _addSeriesService.AddSeries(addSeries);
            }
        }

        protected override bool ShouldDelete(Issue local)
        {
            // not manually added and has no files
            return local.AddOptions.AddType != IssueAddType.Manual &&
                !_mediaFileService.GetFilesByIssue(local.Id).Any();
        }

        protected override void LogProgress(Issue local)
        {
            _logger.ProgressInfo("Updating Info for {0}", local.Title);
        }

        protected override bool IsMerge(Issue local, Issue remote)
        {
            return local.ForeignIssueId != remote.ForeignIssueId;
        }

        protected override UpdateResult UpdateEntity(Issue local, Issue remote)
        {
            UpdateResult result;

            remote.UseDbFieldsFrom(local);

            if (local.Title != (remote.Title ?? "Unknown") ||
                local.ForeignIssueId != remote.ForeignIssueId ||
                local.SeriesMetadata.Value.ForeignSeriesId != remote.SeriesMetadata.Value.ForeignSeriesId)
            {
                result = UpdateResult.UpdateTags;
            }
            else if (!local.Equals(remote))
            {
                result = UpdateResult.Standard;
            }
            else
            {
                result = UpdateResult.None;
            }

            local.UseMetadataFrom(remote);

            local.SeriesMetadataId = remote.SeriesMetadata.Value.Id;
            local.LastInfoSync = DateTime.UtcNow;

            return result;
        }

        protected override UpdateResult MergeEntity(Issue local, Issue target, Issue remote)
        {
            _logger.Warn($"Issue {local} was merged with {remote} because the original was a duplicate.");

            // Update issue ids for files
            var files = _mediaFileService.GetFilesByIssue(local.Id);
            files.ForEach(x => x.IssueId = target.Id);
            _mediaFileService.Update(files);

            // Update issue ids for history
            var items = _historyService.GetByIssue(local.Id, null);
            items.ForEach(x => x.IssueId = target.Id);
            _historyService.UpdateMany(items);

            // Finally delete the old issue
            _issueService.DeleteMany(new List<Issue> { local });

            return UpdateResult.UpdateTags;
        }

        protected override Issue GetEntityByForeignId(Issue local)
        {
            return _issueService.FindById(local.ForeignIssueId);
        }

        protected override void SaveEntity(Issue local)
        {
            // Use UpdateMany to avoid firing the issue edited event
            _issueService.UpdateMany(new List<Issue> { local });
        }

        protected override void DeleteEntity(Issue local, bool deleteFiles)
        {
            _issueService.DeleteIssue(local.Id, deleteFiles);
        }

        protected override List<object> GetRemoteChildren(Issue local, Issue remote)
        {
            return new List<object>();
        }

        protected override List<object> GetLocalChildren(Issue entity, List<object> remoteChildren)
        {
            return new List<object>();
        }

        protected override Tuple<object, List<object>> GetMatchingExistingChildren(List<object> existingChildren, object remote)
        {
            return Tuple.Create((object)null, new List<object>());
        }

        protected override void PrepareNewChild(object child, Issue entity)
        {
        }

        protected override void PrepareExistingChild(object local, object remote, Issue entity)
        {
        }

        protected override void AddChildren(List<object> children)
        {
        }

        protected override bool RefreshChildren(SortedChildren localChildren, List<object> remoteChildren, Series remoteData, bool forceChildRefresh, bool forceUpdateFileTags, DateTime? lastUpdate)
        {
            return false;
        }

        protected override void PublishEntityUpdatedEvent(Issue entity)
        {
            // Fetch fresh from DB so all lazy loads are available
            _eventAggregator.PublishEvent(new IssueUpdatedEvent(_issueService.GetIssue(entity.Id)));
        }

        public bool RefreshIssueInfo(List<Issue> issues, List<Issue> remoteIssues, Series remoteData, bool forceIssueRefresh, bool forceUpdateFileTags, DateTime? lastUpdate)
        {
            var updated = false;

            foreach (var issue in issues)
            {
                if (forceIssueRefresh || _checkIfIssueShouldBeRefreshed.ShouldRefresh(issue))
                {
                    updated |= RefreshIssueInfo(issue, remoteIssues, remoteData, forceUpdateFileTags);
                }
                else
                {
                    _logger.Debug("Skipping refresh of issue: {0}", issue.Title);
                }
            }

            return updated;
        }

        public bool RefreshIssueInfo(Issue issue, List<Issue> remoteIssues, Series remoteData, bool forceUpdateFileTags)
        {
            return RefreshEntityInfo(issue, remoteIssues, remoteData, true, forceUpdateFileTags, null);
        }

        public bool RefreshIssueInfo(Issue issue)
        {
            var data = GetSkyhookData(issue);

            return RefreshIssueInfo(issue, data.Issues, data, false);
        }

        public void Execute(BulkRefreshIssueCommand message)
        {
            var issues = _issueService.GetIssues(message.IssueIds);

            foreach (var issue in issues)
            {
                RefreshIssueInfo(issue);
            }
        }

        public void Execute(RefreshIssueCommand message)
        {
            if (message.IssueId.HasValue)
            {
                var issue = _issueService.GetIssue(message.IssueId.Value);

                RefreshIssueInfo(issue);

                var files = _mediaFileService.GetFilesByIssue(issue.Id);

                foreach (var file in files)
                {
                    _comicInfoEmbedService.EmbedMetadata(file);
                }
            }
        }
    }
}
