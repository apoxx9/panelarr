using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Issues;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.SeriesStats;
using NzbDrone.SignalR;
using Panelarr.Api.V1.Issues;
using Panelarr.Http;
using Panelarr.Http.Extensions;

namespace Panelarr.Api.V1.Calendar
{
    [V1ApiController]
    public class CalendarController : IssueControllerWithSignalR
    {
        public CalendarController(IIssueService issueService,
                              ISeriesIssueLinkService seriesIssueLinkService,
                              ISeriesStatisticsService seriesStatisticsService,
                              IMapCoversToLocal coverMapper,
                              IUpgradableSpecification upgradableSpecification,
                              IBroadcastSignalRMessage signalRBroadcaster)
        : base(issueService, seriesIssueLinkService, seriesStatisticsService, coverMapper, upgradableSpecification, signalRBroadcaster)
        {
        }

        [HttpGet]
        public List<IssueResource> GetCalendar(DateTime? start, DateTime? end, bool unmonitored = false, bool includeSeries = false)
        {
            //TODO: Add Issue Image support to IssueControllerWithSignalR
            var includeIssueImages = Request.GetBooleanQueryParameter("includeIssueImages");

            var startUse = start ?? DateTime.Today;
            var endUse = end ?? DateTime.Today.AddDays(2);

            var resources = MapToResource(_issueService.IssuesBetweenDates(startUse, endUse, unmonitored), includeSeries);

            return resources.OrderBy(e => e.ReleaseDate).ToList();
        }
    }
}
