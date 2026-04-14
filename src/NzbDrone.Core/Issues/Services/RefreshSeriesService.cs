using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.History;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Issues.Events;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Profiles.Metadata;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.Issues
{
    public interface IRefreshSeriesService
    {
    }

    public class RefreshSeriesService : RefreshEntityServiceBase<Series, Issue>,
        IRefreshSeriesService,
        IExecute<RefreshSeriesCommand>,
        IExecute<BulkRefreshSeriesCommand>
    {
        private readonly IProvideSeriesInfo _seriesInfo;
        private readonly ISeriesService _seriesService;
        private readonly IIssueService _issueService;
        private readonly IMetadataProfileService _metadataProfileService;
        private readonly IRefreshIssueService _refreshIssueService;
        private readonly IRefreshSeriesGroupService _refreshSeriesGroupService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly IMediaFileService _mediaFileService;
        private readonly IHistoryService _historyService;
        private readonly IRootFolderService _rootFolderService;
        private readonly ICheckIfSeriesShouldBeRefreshed _checkIfSeriesShouldBeRefreshed;
        private readonly IMonitorNewIssueService _monitorNewIssueService;
        private readonly IConfigService _configService;
        private readonly IImportListExclusionService _importListExclusionService;
        private readonly Logger _logger;

        public RefreshSeriesService(IProvideSeriesInfo seriesInfo,
                                    ISeriesService seriesService,
                                    ISeriesMetadataService seriesMetadataService,
                                    IIssueService issueService,
                                    IMetadataProfileService metadataProfileService,
                                    IRefreshIssueService refreshIssueService,
                                    IRefreshSeriesGroupService refreshSeriesGroupService,
                                    IEventAggregator eventAggregator,
                                    IManageCommandQueue commandQueueManager,
                                    IMediaFileService mediaFileService,
                                    IHistoryService historyService,
                                    IRootFolderService rootFolderService,
                                    ICheckIfSeriesShouldBeRefreshed checkIfSeriesShouldBeRefreshed,
                                    IMonitorNewIssueService monitorNewIssueService,
                                    IConfigService configService,
                                    IImportListExclusionService importListExclusionService,
                                    Logger logger)
        : base(logger, seriesMetadataService)
        {
            _seriesInfo = seriesInfo;
            _seriesService = seriesService;
            _issueService = issueService;
            _metadataProfileService = metadataProfileService;
            _refreshIssueService = refreshIssueService;
            _refreshSeriesGroupService = refreshSeriesGroupService;
            _eventAggregator = eventAggregator;
            _commandQueueManager = commandQueueManager;
            _mediaFileService = mediaFileService;
            _historyService = historyService;
            _rootFolderService = rootFolderService;
            _checkIfSeriesShouldBeRefreshed = checkIfSeriesShouldBeRefreshed;
            _monitorNewIssueService = monitorNewIssueService;
            _configService = configService;
            _importListExclusionService = importListExclusionService;
            _logger = logger;
        }

        private Series GetSkyhookData(string foreignId)
        {
            try
            {
                return _seriesInfo.GetSeriesInfo(foreignId);
            }
            catch (SeriesNotFoundException)
            {
                _logger.Error($"Could not find series with id {foreignId}");
            }

            return null;
        }

        protected override RemoteData GetRemoteData(Series local, List<Series> remote, Series data)
        {
            var result = new RemoteData();

            if (data != null)
            {
                result.Entity = data;
                result.Metadata = new List<SeriesMetadata> { data.Metadata.Value };
            }

            return result;
        }

        protected override bool ShouldDelete(Series local)
        {
            return !_mediaFileService.GetFilesBySeries(local.Id).Any();
        }

        protected override void LogProgress(Series local)
        {
            _logger.ProgressInfo("Updating Info for {0}", local.Name);
        }

        protected override bool IsMerge(Series local, Series remote)
        {
            _logger.Trace($"local: {local.SeriesMetadataId} remote: {remote.Metadata.Value.Id}");
            return local.SeriesMetadataId != remote.Metadata.Value.Id;
        }

        protected override UpdateResult UpdateEntity(Series local, Series remote)
        {
            var result = UpdateResult.None;

            if (!local.Metadata.Value.Equals(remote.Metadata.Value))
            {
                result = UpdateResult.UpdateTags;
            }

            local.UseMetadataFrom(remote);
            local.Metadata = remote.Metadata;
            local.LastInfoSync = DateTime.UtcNow;

            try
            {
                local.Path = new DirectoryInfo(local.Path).FullName;
                local.Path = local.Path.GetActualCasing();
            }
            catch (Exception e)
            {
                _logger.Warn(e, "Couldn't update series path for " + local.Path);
            }

            return result;
        }

        protected override UpdateResult MoveEntity(Series local, Series remote)
        {
            _logger.Debug($"Updating foreign id for {local} to {remote}");

            // We are moving from one metadata to another (will already have been poplated)
            local.SeriesMetadataId = remote.Metadata.Value.Id;
            local.Metadata = remote.Metadata.Value;

            // Update list exclusion if one exists
            var importExclusion = _importListExclusionService.FindByForeignId(local.Metadata.Value.ForeignSeriesId);

            if (importExclusion != null)
            {
                importExclusion.ForeignId = remote.Metadata.Value.ForeignSeriesId;
                _importListExclusionService.Update(importExclusion);
            }

            // Do the standard update
            UpdateEntity(local, remote);

            // We know we need to update tags as series id has changed
            return UpdateResult.UpdateTags;
        }

        protected override UpdateResult MergeEntity(Series local, Series target, Series remote)
        {
            _logger.Warn($"Series {local} was replaced with {remote} because the original was a duplicate.");

            // Update list exclusion if one exists
            var importExclusionLocal = _importListExclusionService.FindByForeignId(local.Metadata.Value.ForeignSeriesId);

            if (importExclusionLocal != null)
            {
                var importExclusionTarget = _importListExclusionService.FindByForeignId(target.Metadata.Value.ForeignSeriesId);
                if (importExclusionTarget == null)
                {
                    importExclusionLocal.ForeignId = remote.Metadata.Value.ForeignSeriesId;
                    _importListExclusionService.Update(importExclusionLocal);
                }
            }

            // move any issues over to the new series and remove the local series
            var issues = _issueService.GetIssuesBySeries(local.Id);
            issues.ForEach(x => x.SeriesMetadataId = target.SeriesMetadataId);
            _issueService.UpdateMany(issues);
            _seriesService.DeleteSeries(local.Id, false);

            // Update history entries to new id
            var items = _historyService.GetBySeries(local.Id, null);
            items.ForEach(x => x.SeriesId = target.Id);
            _historyService.UpdateMany(items);

            // We know we need to update tags as series id has changed
            return UpdateResult.UpdateTags;
        }

        protected override Series GetEntityByForeignId(Series local)
        {
            return _seriesService.FindById(local.ForeignSeriesId);
        }

        protected override void SaveEntity(Series local)
        {
            _seriesService.UpdateSeries(local);
        }

        protected override void DeleteEntity(Series local, bool deleteFiles)
        {
            _seriesService.DeleteSeries(local.Id, deleteFiles);
        }

        protected override List<Issue> GetRemoteChildren(Series local, Series remote)
        {
            // MetadataProfileId has been removed from Series; get all remote issues unfiltered
            var filtered = remote.Issues.Value;

            var all = filtered.DistinctBy(m => m.ForeignIssueId).ToList();
            var ids = all.Select(x => x.ForeignIssueId).ToList();
            var excluded = _importListExclusionService.FindByForeignId(ids).Select(x => x.ForeignId).ToList();
            return all.Where(x => !excluded.Contains(x.ForeignIssueId)).ToList();
        }

        protected override List<Issue> GetLocalChildren(Series entity, List<Issue> remoteChildren)
        {
            return _issueService.GetIssuesForRefresh(entity.SeriesMetadataId,
                                                     remoteChildren.Select(x => x.ForeignIssueId).ToList());
        }

        protected override Tuple<Issue, List<Issue>> GetMatchingExistingChildren(List<Issue> existingChildren, Issue remote)
        {
            var existingChild = existingChildren.SingleOrDefault(x => x.ForeignIssueId == remote.ForeignIssueId);
            var mergeChildren = new List<Issue>();
            return Tuple.Create(existingChild, mergeChildren);
        }

        protected override void PrepareNewChild(Issue child, Series entity)
        {
            child.Series = entity;
            child.SeriesMetadata = entity.Metadata.Value;
            child.SeriesMetadataId = entity.Metadata.Value.Id;
            child.Added = DateTime.UtcNow;
            child.LastInfoSync = DateTime.MinValue;
            child.Monitored = entity.Monitored;
        }

        protected override void PrepareExistingChild(Issue local, Issue remote, Series entity)
        {
            local.Series = entity;
            local.SeriesMetadata = entity.Metadata.Value;
            local.SeriesMetadataId = entity.Metadata.Value.Id;

            remote.UseDbFieldsFrom(local);
        }

        protected override void ProcessChildren(Series entity, SortedChildren children)
        {
            foreach (var issue in children.Added)
            {
                issue.Monitored = _monitorNewIssueService.ShouldMonitorNewIssue(issue, children.UpToDate, entity.MonitorNewItems);
            }
        }

        protected override void AddChildren(List<Issue> children)
        {
            foreach (var child in children)
            {
                if (child.TitleSlug == null)
                {
                    _logger.Warn("Issue {0} ({1}) has null TitleSlug, setting from ForeignIssueId", child.ForeignIssueId, child.Title);
                    child.TitleSlug = child.ForeignIssueId ?? child.Title ?? "unknown";
                }
            }

            _issueService.InsertMany(children);
        }

        protected override bool RefreshChildren(SortedChildren localChildren, List<Issue> remoteChildren, Series remoteData, bool forceChildRefresh, bool forceUpdateFileTags, DateTime? lastUpdate)
        {
            return _refreshIssueService.RefreshIssueInfo(localChildren.All, remoteChildren, remoteData, forceChildRefresh, forceUpdateFileTags, lastUpdate);
        }

        protected override void PublishEntityUpdatedEvent(Series entity)
        {
            _eventAggregator.PublishEvent(new SeriesUpdatedEvent(entity));
        }

        protected override void PublishRefreshCompleteEvent(Series entity)
        {
            // little hack - trigger the series group update here
            var seriesGroups = entity.SeriesGroups?.Value ?? new System.Collections.Generic.List<SeriesGroup>();
            _refreshSeriesGroupService.RefreshSeriesInfo(entity.SeriesMetadataId, seriesGroups, entity, false, false, null);
            _eventAggregator.PublishEvent(new SeriesRefreshCompleteEvent(entity));
        }

        protected override void PublishChildrenUpdatedEvent(Series entity, List<Issue> newChildren, List<Issue> updateChildren, List<Issue> deleteChildren)
        {
            _eventAggregator.PublishEvent(new IssueInfoRefreshedEvent(entity, newChildren, updateChildren, deleteChildren));
        }

        private void Rescan(List<int> seriesIds, bool isNew, CommandTrigger trigger, bool infoUpdated)
        {
            var rescanAfterRefresh = _configService.RescanAfterRefresh;
            var shouldRescan = true;

            if (isNew)
            {
                _logger.Trace("Forcing rescan. Reason: New series added");
                shouldRescan = true;
            }
            else if (rescanAfterRefresh == RescanAfterRefreshType.Never)
            {
                _logger.Trace("Skipping rescan. Reason: never rescan after refresh");
                shouldRescan = false;
            }
            else if (rescanAfterRefresh == RescanAfterRefreshType.AfterManual && trigger != CommandTrigger.Manual)
            {
                _logger.Trace("Skipping rescan. Reason: not after automatic refreshes");
                shouldRescan = false;
            }
            else if (!infoUpdated)
            {
                _logger.Trace("Skipping rescan. Reason: no metadata updated");
                shouldRescan = false;
            }

            if (shouldRescan)
            {
                // Only scan the specific series folder, not all root folders
                var seriesPaths = _seriesService.GetSeries(seriesIds)
                    .Where(s => s.Path.IsNotNullOrWhiteSpace())
                    .Select(s => s.Path)
                    .ToList();

                if (seriesPaths.Any())
                {
                    _commandQueueManager.Push(new RescanFoldersCommand(seriesPaths, FilterFilesType.Matched, false, seriesIds));
                }
            }
        }

        private void RefreshSelectedSeries(List<int> seriesIds, bool isNew, CommandTrigger trigger)
        {
            var updated = false;
            var allSeries = _seriesService.GetSeries(seriesIds);

            foreach (var series in allSeries)
            {
                try
                {
                    var data = GetSkyhookData(series.ForeignSeriesId);
                    updated |= RefreshEntityInfo(series, null, data, true, false, null);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't refresh info for {0}", series);
                }
            }

            Rescan(seriesIds, isNew, trigger, updated);
        }

        public void Execute(BulkRefreshSeriesCommand message)
        {
            RefreshSelectedSeries(message.SeriesIds, message.AreNewSeries, message.Trigger);
        }

        public void Execute(RefreshSeriesCommand message)
        {
            var trigger = message.Trigger;
            var isNew = message.IsNewSeries;

            if (message.SeriesId.HasValue)
            {
                RefreshSelectedSeries(new List<int> { message.SeriesId.Value }, isNew, trigger);
            }
            else
            {
                var updated = false;
                var allSeries = _seriesService.GetAllSeries().OrderBy(c => c.Name).ToList();
                var seriesIds = allSeries.Select(x => x.Id).ToList();

                var updatedSeries = new HashSet<string>();

                if (message.LastExecutionTime.HasValue && message.LastExecutionTime.Value.AddDays(14) > DateTime.UtcNow)
                {
                    updatedSeries = _seriesInfo.GetChangedSeries(message.LastStartTime.Value);
                }

                foreach (var series in allSeries)
                {
                    var manualTrigger = message.Trigger == CommandTrigger.Manual;

                    if ((updatedSeries == null && _checkIfSeriesShouldBeRefreshed.ShouldRefresh(series)) ||
                        (updatedSeries != null && updatedSeries.Contains(series.ForeignSeriesId)) ||
                        manualTrigger)
                    {
                        try
                        {
                            LogProgress(series);
                            var data = GetSkyhookData(series.ForeignSeriesId);
                            updated |= RefreshEntityInfo(series, null, data, manualTrigger, false, message.LastStartTime);
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, "Couldn't refresh info for {0}", series);
                        }
                    }
                    else
                    {
                        _logger.Info("Skipping refresh of series: {0}", series.Name);
                    }
                }

                Rescan(seriesIds, isNew, trigger, updated);
            }
        }
    }
}
