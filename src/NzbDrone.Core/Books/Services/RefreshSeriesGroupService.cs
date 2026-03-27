using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Books
{
    public interface IRefreshSeriesGroupService
    {
        bool RefreshSeriesInfo(int authorMetadataId, List<SeriesGroup> remoteBooks, Series remoteData, bool forceBookRefresh, bool forceUpdateFileTags, DateTime? lastUpdate);
    }

    public class RefreshSeriesGroupService : RefreshEntityServiceBase<SeriesGroup, SeriesGroupLink>, IRefreshSeriesGroupService
    {
        private readonly IBookService _bookService;
        private readonly ISeriesGroupService _seriesService;
        private readonly ISeriesBookLinkService _linkService;
        private readonly IRefreshSeriesBookLinkService _refreshLinkService;
        private readonly Logger _logger;

        public RefreshSeriesGroupService(IBookService bookService,
                                    ISeriesGroupService seriesService,
                                    ISeriesBookLinkService linkService,
                                    IRefreshSeriesBookLinkService refreshLinkService,
                                    ISeriesMetadataService authorMetadataService,
                                    Logger logger)
        : base(logger, authorMetadataService)
        {
            _bookService = bookService;
            _seriesService = seriesService;
            _linkService = linkService;
            _refreshLinkService = refreshLinkService;
            _logger = logger;
        }

        protected override RemoteData GetRemoteData(SeriesGroup local, List<SeriesGroup> remote, Series data)
        {
            return new RemoteData
            {
                Entity = remote.SingleOrDefault(x => x.ForeignSeriesId == local.ForeignSeriesId)
            };
        }

        protected override bool IsMerge(SeriesGroup local, SeriesGroup remote)
        {
            return local.ForeignSeriesId != remote.ForeignSeriesId;
        }

        protected override UpdateResult UpdateEntity(SeriesGroup local, SeriesGroup remote)
        {
            if (local.Equals(remote))
            {
                return UpdateResult.None;
            }

            local.UseMetadataFrom(remote);

            return UpdateResult.UpdateTags;
        }

        protected override SeriesGroup GetEntityByForeignId(SeriesGroup local)
        {
            return _seriesService.FindById(local.ForeignSeriesId);
        }

        protected override void SaveEntity(SeriesGroup local)
        {
            // Use UpdateMany to avoid firing the issue edited event
            _seriesService.UpdateMany(new List<SeriesGroup> { local });
        }

        protected override void DeleteEntity(SeriesGroup local, bool deleteFiles)
        {
            _logger.Trace($"Removing links for series {local} author {local.ForeignSeriesId}");
            var children = GetLocalChildren(local, null);
            _linkService.DeleteMany(children);

            if (!_linkService.GetLinksBySeries(local.Id).Any())
            {
                _logger.Trace($"SeriesGroup {local} has no links remaining, removing");
                _seriesService.Delete(local.Id);
            }
        }

        protected override List<SeriesGroupLink> GetRemoteChildren(SeriesGroup local, SeriesGroup remote)
        {
            return remote.LinkItems;
        }

        protected override List<SeriesGroupLink> GetLocalChildren(SeriesGroup entity, List<SeriesGroupLink> remoteChildren)
        {
            return _linkService.GetLinksBySeriesAndSeries(entity.Id, entity.ForeignSeriesId);
        }

        protected override Tuple<SeriesGroupLink, List<SeriesGroupLink>> GetMatchingExistingChildren(List<SeriesGroupLink> existingChildren, SeriesGroupLink remote)
        {
            var existingChild = existingChildren.SingleOrDefault(x => x.IssueId == remote.Issue.Value.Id);
            var mergeChildren = new List<SeriesGroupLink>();
            return Tuple.Create(existingChild, mergeChildren);
        }

        protected override void PrepareNewChild(SeriesGroupLink child, SeriesGroup entity)
        {
            child.SeriesGroup = entity;
            child.SeriesId = entity.Id;
            child.IssueId = child.Issue.Value.Id;
        }

        protected override void PrepareExistingChild(SeriesGroupLink local, SeriesGroupLink remote, SeriesGroup entity)
        {
            local.SeriesGroup = entity;
            local.SeriesId = entity.Id;

            remote.Id = local.Id;
            remote.IssueId = local.IssueId;
            remote.SeriesId = entity.Id;
        }

        protected override void AddChildren(List<SeriesGroupLink> children)
        {
            _linkService.InsertMany(children);
        }

        protected override bool RefreshChildren(SortedChildren localChildren, List<SeriesGroupLink> remoteChildren, Series remoteData, bool forceChildRefresh, bool forceUpdateFileTags, DateTime? lastUpdate)
        {
            return _refreshLinkService.RefreshSeriesBookLinkInfo(localChildren.Added, localChildren.Updated, localChildren.Merged, localChildren.Deleted, localChildren.UpToDate, remoteChildren, forceUpdateFileTags);
        }

        public bool RefreshSeriesInfo(int authorMetadataId, List<SeriesGroup> remoteSeries, Series remoteData, bool forceBookRefresh, bool forceUpdateFileTags, DateTime? lastUpdate)
        {
            var updated = false;

            var existingByMetadata = _seriesService.GetBySeriesMetadataId(authorMetadataId);
            var existingByForeignId = _seriesService.FindById(remoteSeries.Select(x => x.ForeignSeriesId).ToList());
            var existing = existingByMetadata.Concat(existingByForeignId).GroupBy(x => x.ForeignSeriesId).Select(x => x.First()).ToList();

            var issues = _bookService.GetBooksBySeriesMetadataId(authorMetadataId);
            var bookDict = issues.ToDictionary(x => x.ForeignIssueId);
            var links = new List<SeriesGroupLink>();

            foreach (var s in remoteData.SeriesGroups?.Value ?? new List<SeriesGroup>())
            {
                s.LinkItems.Value.ForEach(x => x.SeriesGroup = s);
                links.AddRange(s.LinkItems.Value.Where(x => bookDict.ContainsKey(x.Issue.Value.ForeignIssueId)));
            }

            var grouped = links.GroupBy(x => x.SeriesGroup.Value);

            // Put in the links that go with the issues we actually have
            foreach (var group in grouped)
            {
                group.Key.LinkItems = group.ToList();
            }

            remoteSeries = grouped.Select(x => x.Key).ToList();

            var toAdd = remoteSeries.ExceptBy(x => x.ForeignSeriesId, existing, x => x.ForeignSeriesId, StringComparer.Ordinal).ToList();
            var all = toAdd.Union(existing).ToList();

            _seriesService.InsertMany(toAdd);

            foreach (var item in all)
            {
                item.ForeignSeriesGroupId = remoteData.ForeignSeriesId;
                updated |= RefreshEntityInfo(item, remoteSeries, remoteData, true, forceUpdateFileTags, null);
            }

            return updated;
        }
    }
}
