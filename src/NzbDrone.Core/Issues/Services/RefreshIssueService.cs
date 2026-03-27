using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Books.Commands;
using NzbDrone.Core.Books.Events;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.Books
{
    public interface IRefreshBookService
    {
        bool RefreshBookInfo(Issue issue, List<Issue> remoteBooks, Series remoteData, bool forceUpdateFileTags);
        bool RefreshBookInfo(List<Issue> issues, List<Issue> remoteBooks, Series remoteData, bool forceBookRefresh, bool forceUpdateFileTags, DateTime? lastUpdate);
    }

    public class RefreshBookService : RefreshEntityServiceBase<Issue, object>,
        IRefreshBookService,
        IExecute<RefreshBookCommand>,
        IExecute<BulkRefreshBookCommand>
    {
        private readonly IBookService _bookService;
        private readonly ISeriesService _authorService;
        private readonly IRootFolderService _rootFolderService;
        private readonly IAddSeriesService _addSeriesService;
        private readonly IProvideSeriesInfo _authorInfo;
        private readonly IProvideBookInfo _bookInfo;
        private readonly IMediaFileService _mediaFileService;
        private readonly IHistoryService _historyService;
        private readonly IEventAggregator _eventAggregator;
        private readonly ICheckIfBookShouldBeRefreshed _checkIfBookShouldBeRefreshed;
        private readonly IMapCoversToLocal _mediaCoverService;
        private readonly Logger _logger;

        public RefreshBookService(IBookService bookService,
                                  ISeriesService authorService,
                                  IRootFolderService rootFolderService,
                                  IAddSeriesService addSeriesService,
                                  ISeriesMetadataService authorMetadataService,
                                  IProvideSeriesInfo authorInfo,
                                  IProvideBookInfo bookInfo,
                                  IMediaFileService mediaFileService,
                                  IHistoryService historyService,
                                  IEventAggregator eventAggregator,
                                  ICheckIfBookShouldBeRefreshed checkIfBookShouldBeRefreshed,
                                  IMapCoversToLocal mediaCoverService,
                                  Logger logger)
        : base(logger, authorMetadataService)
        {
            _bookService = bookService;
            _authorService = authorService;
            _rootFolderService = rootFolderService;
            _addSeriesService = addSeriesService;
            _authorInfo = authorInfo;
            _bookInfo = bookInfo;
            _mediaFileService = mediaFileService;
            _historyService = historyService;
            _eventAggregator = eventAggregator;
            _checkIfBookShouldBeRefreshed = checkIfBookShouldBeRefreshed;
            _mediaCoverService = mediaCoverService;
            _logger = logger;
        }

        private Series GetSkyhookData(Issue issue)
        {
            try
            {
                var tuple = _bookInfo.GetBookInfo(issue.ForeignIssueId);
                var author = _authorInfo.GetSeriesInfo(tuple.Item1);
                var newbook = tuple.Item2;

                newbook.Series = author;
                newbook.SeriesMetadata = author.Metadata.Value;
                newbook.SeriesMetadataId = issue.SeriesMetadataId;
                newbook.SeriesMetadata.Value.Id = issue.SeriesMetadataId;

                author.Books = new List<Issue> { newbook };
                return author;
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
                issue = data.Books.Value.SingleOrDefault(x => x.ForeignIssueId == local.ForeignIssueId);
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
            // Make sure the appropriate author exists (it could be that an issue changes parent)
            // The authorMetadata entry will be in the db but make sure a corresponding author is too
            // so that the issue doesn't just disappear.

            // TODO filter by metadata id before hitting database
            _logger.Trace($"Ensuring parent author exists [{remote.SeriesMetadata.Value.ForeignSeriesId}]");

            var newSeries = _authorService.FindById(remote.SeriesMetadata.Value.ForeignSeriesId);

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
                _logger.Debug($"Adding missing parent author {addSeries}");
                _addSeriesService.AddSeries(addSeries);
            }
        }

        protected override bool ShouldDelete(Issue local)
        {
            // not manually added and has no files
            return local.AddOptions.AddType != IssueAddType.Manual &&
                !_mediaFileService.GetFilesByBook(local.Id).Any();
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
            var files = _mediaFileService.GetFilesByBook(local.Id);
            files.ForEach(x => x.IssueId = target.Id);
            _mediaFileService.Update(files);

            // Update issue ids for history
            var items = _historyService.GetByBook(local.Id, null);
            items.ForEach(x => x.IssueId = target.Id);
            _historyService.UpdateMany(items);

            // Finally delete the old issue
            _bookService.DeleteMany(new List<Issue> { local });

            return UpdateResult.UpdateTags;
        }

        protected override Issue GetEntityByForeignId(Issue local)
        {
            return _bookService.FindById(local.ForeignIssueId);
        }

        protected override void SaveEntity(Issue local)
        {
            // Use UpdateMany to avoid firing the issue edited event
            _bookService.UpdateMany(new List<Issue> { local });
        }

        protected override void DeleteEntity(Issue local, bool deleteFiles)
        {
            _bookService.DeleteBook(local.Id, deleteFiles);
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
            _eventAggregator.PublishEvent(new IssueUpdatedEvent(_bookService.GetBook(entity.Id)));
        }

        public bool RefreshBookInfo(List<Issue> issues, List<Issue> remoteBooks, Series remoteData, bool forceBookRefresh, bool forceUpdateFileTags, DateTime? lastUpdate)
        {
            var updated = false;

            foreach (var issue in issues)
            {
                if (forceBookRefresh || _checkIfBookShouldBeRefreshed.ShouldRefresh(issue))
                {
                    updated |= RefreshBookInfo(issue, remoteBooks, remoteData, forceUpdateFileTags);
                }
                else
                {
                    _logger.Debug("Skipping refresh of issue: {0}", issue.Title);
                }
            }

            return updated;
        }

        public bool RefreshBookInfo(Issue issue, List<Issue> remoteBooks, Series remoteData, bool forceUpdateFileTags)
        {
            return RefreshEntityInfo(issue, remoteBooks, remoteData, true, forceUpdateFileTags, null);
        }

        public bool RefreshBookInfo(Issue issue)
        {
            var data = GetSkyhookData(issue);

            return RefreshBookInfo(issue, data.Books, data, false);
        }

        public void Execute(BulkRefreshBookCommand message)
        {
            var issues = _bookService.GetBooks(message.IssueIds);

            foreach (var issue in issues)
            {
                RefreshBookInfo(issue);
            }
        }

        public void Execute(RefreshBookCommand message)
        {
            if (message.IssueId.HasValue)
            {
                var issue = _bookService.GetBook(message.IssueId.Value);

                RefreshBookInfo(issue);
            }
        }
    }
}
