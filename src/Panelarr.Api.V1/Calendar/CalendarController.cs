using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Books;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.SeriesStats;
using NzbDrone.SignalR;
using Panelarr.Api.V1.Books;
using Panelarr.Http;
using Panelarr.Http.Extensions;

namespace Panelarr.Api.V1.Calendar
{
    [V1ApiController]
    public class CalendarController : IssueControllerWithSignalR
    {
        public CalendarController(IBookService bookService,
                              ISeriesBookLinkService seriesBookLinkService,
                              ISeriesStatisticsService authorStatisticsService,
                              IMapCoversToLocal coverMapper,
                              IUpgradableSpecification upgradableSpecification,
                              IBroadcastSignalRMessage signalRBroadcaster)
        : base(bookService, seriesBookLinkService, authorStatisticsService, coverMapper, upgradableSpecification, signalRBroadcaster)
        {
        }

        [HttpGet]
        public List<IssueResource> GetCalendar(DateTime? start, DateTime? end, bool unmonitored = false, bool includeSeries = false)
        {
            //TODO: Add Issue Image support to IssueControllerWithSignalR
            var includeBookImages = Request.GetBooleanQueryParameter("includeBookImages");

            var startUse = start ?? DateTime.Today;
            var endUse = end ?? DateTime.Today.AddDays(2);

            var resources = MapToResource(_bookService.IssuesBetweenDates(startUse, endUse, unmonitored), includeSeries);

            return resources.OrderBy(e => e.ReleaseDate).ToList();
        }
    }
}
