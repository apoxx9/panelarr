using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.SeriesStats;
using NzbDrone.SignalR;
using Panelarr.Api.V1.Issues;
using Panelarr.Http;
using Panelarr.Http.Extensions;

namespace Panelarr.Api.V1.Wanted
{
    [V1ApiController("wanted/cutoff")]
    public class CutoffController : IssueControllerWithSignalR
    {
        private readonly IIssueCutoffService _issueCutoffService;

        public CutoffController(IIssueCutoffService issueCutoffService,
                            IIssueService issueService,
                            ISeriesIssueLinkService seriesIssueLinkService,
                            ISeriesStatisticsService seriesStatisticsService,
                            IMapCoversToLocal coverMapper,
                            IUpgradableSpecification upgradableSpecification,
                            IBroadcastSignalRMessage signalRBroadcaster)
        : base(issueService, seriesIssueLinkService, seriesStatisticsService, coverMapper, upgradableSpecification, signalRBroadcaster)
        {
            _issueCutoffService = issueCutoffService;
        }

        [HttpGet]
        public PagingResource<IssueResource> GetCutoffUnmetIssues([FromQuery] PagingRequestResource paging, bool includeSeries = false, bool monitored = true)
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

            return pagingSpec.ApplyToPage(_issueCutoffService.IssuesWhereCutoffUnmet, v => MapToResource(v, includeSeries));
        }
    }
}
