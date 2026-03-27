using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.SeriesStats;
using Panelarr.Api.V1.Series;
using Panelarr.Http;
using Panelarr.Http.Extensions;

namespace Panelarr.Api.V1.Books
{
    /// <summary>
    /// Library sub-endpoints under /api/v1/issue:
    ///   GET /api/v1/issue/missing  — paginated missing issues
    ///   GET /api/v1/issue/calendar — issues with release dates in a date range
    /// </summary>
    [V1ApiController("issue")]
    public class IssueLibraryController : Controller
    {
        private readonly IBookService _bookService;
        private readonly ISeriesBookLinkService _seriesBookLinkService;
        private readonly ISeriesStatisticsService _seriesStatisticsService;
        private readonly IMapCoversToLocal _coverMapper;

        public IssueLibraryController(IBookService bookService,
                                      ISeriesBookLinkService seriesBookLinkService,
                                      ISeriesStatisticsService seriesStatisticsService,
                                      IMapCoversToLocal coverMapper)
        {
            _bookService = bookService;
            _seriesBookLinkService = seriesBookLinkService;
            _seriesStatisticsService = seriesStatisticsService;
            _coverMapper = coverMapper;
        }

        /// <summary>GET /api/v1/issue/missing</summary>
        [HttpGet("missing")]
        public PagingResource<IssueResource> GetMissingIssues([FromQuery] PagingRequestResource paging, bool includeSeries = false, bool monitored = true)
        {
            var pagingResource = new PagingResource<IssueResource>(paging);
            var pagingSpec = new PagingSpec<Issue>
            {
                Page = pagingResource.Page,
                PageSize = pagingResource.PageSize,
                SortKey = pagingResource.SortKey,
                SortDirection = pagingResource.SortDirection
            };

            if (monitored)
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Series.Value.Monitored == true);
            }
            else
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == false || v.Series.Value.Monitored == false);
            }

            return pagingSpec.ApplyToPage(_bookService.IssuesWithoutFiles, v => MapToResource(v, includeSeries));
        }

        /// <summary>GET /api/v1/issue/calendar</summary>
        [HttpGet("calendar")]
        public List<IssueResource> GetCalendar(DateTime? start, DateTime? end, bool unmonitored = false, bool includeSeries = false)
        {
            var startUse = start ?? DateTime.Today;
            var endUse = end ?? DateTime.Today.AddDays(14);

            var resources = MapToResource(_bookService.IssuesBetweenDates(startUse, endUse, unmonitored), includeSeries);

            return resources.OrderBy(e => e.ReleaseDate).ToList();
        }

        private IssueResource MapToResource(Issue issue, bool includeSeries)
        {
            var resource = issue.ToResource();

            if (includeSeries)
            {
                var author = issue.Series.Value;
                resource.Series = author.ToResource();
            }

            _coverMapper.ConvertToLocalUrls(resource.Id, MediaCoverEntity.Issue, resource.Images);

            return resource;
        }

        private List<IssueResource> MapToResource(List<Issue> issues, bool includeSeries)
        {
            var seriesLinks = _seriesBookLinkService.GetLinksByBook(issues.Select(x => x.SeriesMetadataId).Distinct().ToList())
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

            var authorStats = _seriesStatisticsService.SeriesStatistics();
            var bookStatsDict = authorStats.SelectMany(x => x.IssueStatistics).ToDictionary(x => x.IssueId);

            foreach (var issueResource in result)
            {
                if (bookStatsDict.TryGetValue(issueResource.Id, out var stats))
                {
                    issueResource.Statistics = stats.ToResource();
                }
            }

            foreach (var issueResource in result)
            {
                _coverMapper.ConvertToLocalUrls(issueResource.Id, MediaCoverEntity.Issue, issueResource.Images);
            }

            return result;
        }
    }
}
