using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Issues;
using Panelarr.Http;

namespace Panelarr.Api.V1.IssueShelf
{
    [V1ApiController]
    public class IssueshelfController : Controller
    {
        private readonly ISeriesService _seriesService;
        private readonly IIssueMonitoredService _issueMonitoredService;

        public IssueshelfController(ISeriesService seriesService, IIssueMonitoredService issueMonitoredService)
        {
            _seriesService = seriesService;
            _issueMonitoredService = issueMonitoredService;
        }

        [HttpPost]
        public IActionResult UpdateAll([FromBody] IssueshelfResource request)
        {
            //Read from request
            var seriesToUpdate = _seriesService.GetSeries(request.Series.Select(s => s.Id));

            foreach (var s in request.Series)
            {
                var series = seriesToUpdate.Single(c => c.Id == s.Id);

                if (s.Monitored.HasValue)
                {
                    series.Monitored = s.Monitored.Value;
                }

                if (request.MonitoringOptions != null && request.MonitoringOptions.Monitor == MonitorTypes.None)
                {
                    series.Monitored = false;
                }

                if (request.MonitorNewItems.HasValue)
                {
                    series.MonitorNewItems = request.MonitorNewItems.Value;
                }

                _issueMonitoredService.SetIssueMonitoredStatus(series, request.MonitoringOptions);
            }

            return Accepted(request);
        }
    }
}
