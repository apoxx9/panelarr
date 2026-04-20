using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Issues.Commands;
using NzbDrone.Core.Messaging.Commands;
using Panelarr.Http;

namespace Panelarr.Api.V1.Series
{
    /// <summary>
    /// Dedicated bulk-operation endpoints for Series.
    /// PUT  /api/v1/series/monitor  — set monitored state on multiple series
    /// </summary>
    [V1ApiController("series")]
    public class SeriesBulkController : Controller
    {
        private readonly ISeriesService _seriesService;
        private readonly IManageCommandQueue _commandQueueManager;

        public SeriesBulkController(ISeriesService seriesService, IManageCommandQueue commandQueueManager)
        {
            _seriesService = seriesService;
            _commandQueueManager = commandQueueManager;
        }

        /// <summary>PUT /api/v1/series/monitor</summary>
        [HttpPut("monitor")]
        public IActionResult SetMonitored([FromBody] SeriesMonitorResource resource)
        {
            var seriesToUpdate = _seriesService.GetSeries(resource.SeriesIds);

            foreach (var series in seriesToUpdate)
            {
                series.Monitored = resource.Monitored;
            }

            _seriesService.UpdateSeries(seriesToUpdate, true);
            return Accepted();
        }

        /// <summary>POST /api/v1/series/rescan — trigger RescanSeries command for multiple series.</summary>
        [HttpPost("rescan")]
        public IActionResult RescanSeries([FromBody] SeriesBulkCommandResource resource)
        {
            foreach (var seriesId in resource.SeriesIds)
            {
                _commandQueueManager.Push(new RefreshSeriesCommand(seriesId));
            }

            return Accepted();
        }

        /// <summary>POST /api/v1/series/search — trigger SeriesSearch for multiple series.</summary>
        [HttpPost("search")]
        public IActionResult SearchSeries([FromBody] SeriesBulkCommandResource resource)
        {
            foreach (var seriesId in resource.SeriesIds)
            {
                _commandQueueManager.Push(new NzbDrone.Core.IndexerSearch.SeriesSearchCommand { SeriesId = seriesId });
            }

            return Accepted();
        }
    }

    public class SeriesMonitorResource
    {
        public List<int> SeriesIds { get; set; }
        public bool Monitored { get; set; }
    }

    public class SeriesBulkCommandResource
    {
        public List<int> SeriesIds { get; set; }
    }
}
