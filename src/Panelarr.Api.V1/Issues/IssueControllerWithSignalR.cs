using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Books;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.SeriesStats;
using NzbDrone.SignalR;
using Panelarr.Api.V1.Series;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Books
{
    public abstract class IssueControllerWithSignalR : RestControllerWithSignalR<IssueResource, Issue>
    {
        protected readonly IBookService _bookService;
        protected readonly ISeriesBookLinkService _seriesBookLinkService;
        protected readonly ISeriesStatisticsService _authorStatisticsService;
        protected readonly IUpgradableSpecification _qualityUpgradableSpecification;
        protected readonly IMapCoversToLocal _coverMapper;

        protected IssueControllerWithSignalR(IBookService bookService,
                                        ISeriesBookLinkService seriesBookLinkService,
                                        ISeriesStatisticsService authorStatisticsService,
                                        IMapCoversToLocal coverMapper,
                                        IUpgradableSpecification qualityUpgradableSpecification,
                                        IBroadcastSignalRMessage signalRBroadcaster)
            : base(signalRBroadcaster)
        {
            _bookService = bookService;
            _seriesBookLinkService = seriesBookLinkService;
            _authorStatisticsService = authorStatisticsService;
            _coverMapper = coverMapper;
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
        }

        protected override IssueResource GetResourceById(int id)
        {
            var issue = _bookService.GetBook(id);
            var resource = MapToResource(issue, true);
            return resource;
        }

        protected override IssueResource GetResourceByIdForBroadcast(int id)
        {
            var issue = _bookService.GetBook(id);
            var resource = MapToResource(issue, false);
            return resource;
        }

        protected IssueResource MapToResource(Issue issue, bool includeSeries)
        {
            var resource = issue.ToResource();

            if (includeSeries)
            {
                var author = issue.Series.Value;

                resource.Series = author.ToResource();
            }

            FetchAndLinkBookStatistics(resource);
            MapCoversToLocal(resource);

            return resource;
        }

        protected List<IssueResource> MapToResource(List<Issue> issues, bool includeSeries)
        {
            var seriesLinks = _seriesBookLinkService.GetLinksByBook(issues.Select(x => x.Id).ToList())
                .GroupBy(x => x.IssueId)
                .ToDictionary(x => x.Key, y => y.ToList());

            foreach (var issue in issues)
            {
                if (seriesLinks.TryGetValue(issue.Id, out var links))
                {
                    issue.SeriesLinks = links;
                }
                else
                {
                    issue.SeriesLinks = new List<SeriesGroupLink>();
                }
            }

            var result = issues.ToResource();

            if (includeSeries)
            {
                var authorDict = new Dictionary<int, NzbDrone.Core.Books.Series>();
                for (var i = 0; i < issues.Count; i++)
                {
                    var issue = issues[i];
                    var resource = result[i];
                    var author = authorDict.GetValueOrDefault(issues[i].SeriesMetadataId) ?? issue.Series?.Value;
                    authorDict[author.SeriesMetadataId] = author;

                    resource.Series = author.ToResource();
                }
            }

            var authorStats = _authorStatisticsService.SeriesStatistics();
            LinkSeriesStatistics(result, authorStats);
            MapCoversToLocal(result.ToArray());

            return result;
        }

        private void FetchAndLinkBookStatistics(IssueResource resource)
        {
            LinkSeriesStatistics(resource, _authorStatisticsService.SeriesStatistics(resource.SeriesId));
        }

        private void LinkSeriesStatistics(List<IssueResource> resources, List<SeriesStatistics> authorStatistics)
        {
            var bookStatsDict = authorStatistics.SelectMany(x => x.IssueStatistics).ToDictionary(x => x.IssueId);

            foreach (var issue in resources)
            {
                if (bookStatsDict.TryGetValue(issue.Id, out var stats))
                {
                    issue.Statistics = stats.ToResource();
                }
            }
        }

        private void LinkSeriesStatistics(IssueResource resource, SeriesStatistics authorStatistics)
        {
            if (authorStatistics?.IssueStatistics != null)
            {
                var dictBookStats = authorStatistics.IssueStatistics.ToDictionary(v => v.IssueId);

                resource.Statistics = dictBookStats.GetValueOrDefault(resource.Id).ToResource();
            }
        }

        private void MapCoversToLocal(params IssueResource[] issues)
        {
            foreach (var bookResource in issues)
            {
                _coverMapper.ConvertToLocalUrls(bookResource.Id, MediaCoverEntity.Issue, bookResource.Images);
            }
        }
    }
}
