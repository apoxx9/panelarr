using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.SeriesStats;
using NzbDrone.SignalR;
using Panelarr.Api.V1.Series;
using Panelarr.Http.REST;

namespace Panelarr.Api.V1.Issues
{
    public abstract class IssueControllerWithSignalR : RestControllerWithSignalR<IssueResource, Issue>
    {
        protected readonly IIssueService _issueService;
        protected readonly ISeriesIssueLinkService _seriesIssueLinkService;
        protected readonly ISeriesStatisticsService _seriesStatisticsService;
        protected readonly IUpgradableSpecification _qualityUpgradableSpecification;
        protected readonly IMapCoversToLocal _coverMapper;

        protected IssueControllerWithSignalR(IIssueService issueService,
                                        ISeriesIssueLinkService seriesIssueLinkService,
                                        ISeriesStatisticsService seriesStatisticsService,
                                        IMapCoversToLocal coverMapper,
                                        IUpgradableSpecification qualityUpgradableSpecification,
                                        IBroadcastSignalRMessage signalRBroadcaster)
            : base(signalRBroadcaster)
        {
            _issueService = issueService;
            _seriesIssueLinkService = seriesIssueLinkService;
            _seriesStatisticsService = seriesStatisticsService;
            _coverMapper = coverMapper;
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
        }

        protected override IssueResource GetResourceById(int id)
        {
            var issue = _issueService.GetIssue(id);
            var resource = MapToResource(issue, true);
            return resource;
        }

        protected override IssueResource GetResourceByIdForBroadcast(int id)
        {
            var issue = _issueService.GetIssue(id);
            var resource = MapToResource(issue, false);
            return resource;
        }

        protected IssueResource MapToResource(Issue issue, bool includeSeries)
        {
            var resource = issue.ToResource();

            if (includeSeries)
            {
                var series = issue.Series.Value;

                resource.Series = series.ToResource();
            }

            FetchAndLinkIssueStatistics(resource);
            MapCoversToLocal(resource);

            return resource;
        }

        protected List<IssueResource> MapToResource(List<Issue> issues, bool includeSeries)
        {
            var seriesLinks = _seriesIssueLinkService.GetLinksByIssue(issues.Select(x => x.SeriesMetadataId).Distinct().ToList())
                .GroupBy(x => x.SeriesMetadataId)
                .ToDictionary(x => x.Key, y => y.ToList());

            foreach (var issue in issues)
            {
                if (seriesLinks.TryGetValue(issue.SeriesMetadataId, out var links))
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
                var seriesDict = new Dictionary<int, NzbDrone.Core.Issues.Series>();
                for (var i = 0; i < issues.Count; i++)
                {
                    var issue = issues[i];
                    var resource = result[i];
                    var series = seriesDict.GetValueOrDefault(issues[i].SeriesMetadataId) ?? issue.Series?.Value;
                    seriesDict[series.SeriesMetadataId] = series;

                    resource.Series = series.ToResource();
                }
            }

            var seriesStats = _seriesStatisticsService.SeriesStatistics();
            LinkSeriesStatistics(result, seriesStats);
            MapCoversToLocal(result.ToArray());

            return result;
        }

        private void FetchAndLinkIssueStatistics(IssueResource resource)
        {
            LinkSeriesStatistics(resource, _seriesStatisticsService.SeriesStatistics(resource.SeriesId));
        }

        private void LinkSeriesStatistics(List<IssueResource> resources, List<SeriesStatistics> seriesStatistics)
        {
            var issueStatsDict = seriesStatistics.SelectMany(x => x.IssueStatistics).ToDictionary(x => x.IssueId);

            foreach (var issue in resources)
            {
                if (issueStatsDict.TryGetValue(issue.Id, out var stats))
                {
                    issue.Statistics = stats.ToResource();
                }
            }
        }

        private void LinkSeriesStatistics(IssueResource resource, SeriesStatistics seriesStatistics)
        {
            if (seriesStatistics?.IssueStatistics != null)
            {
                var dictIssueStats = seriesStatistics.IssueStatistics.ToDictionary(v => v.IssueId);

                resource.Statistics = dictIssueStats.GetValueOrDefault(resource.Id).ToResource();
            }
        }

        private void MapCoversToLocal(params IssueResource[] issues)
        {
            foreach (var issueResource in issues)
            {
                _coverMapper.ConvertToLocalUrls(issueResource.Id, MediaCoverEntity.Issue, issueResource.Images);
            }
        }
    }
}
