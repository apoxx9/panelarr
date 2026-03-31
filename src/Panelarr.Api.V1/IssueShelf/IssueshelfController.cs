using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Issues;
using Panelarr.Http;

namespace Panelarr.Api.V1.IssueShelf
{
    [V1ApiController]
    public class IssueshelfController : Controller
    {
        private readonly ISeriesService _authorService;
        private readonly IIssueMonitoredService _issueMonitoredService;

        public IssueshelfController(ISeriesService authorService, IIssueMonitoredService bookMonitoredService)
        {
            _authorService = authorService;
            _issueMonitoredService = bookMonitoredService;
        }

        [HttpPost]
        public IActionResult UpdateAll([FromBody] IssueshelfResource request)
        {
            //Read from request
            var authorToUpdate = _authorService.GetSeriess(request.Seriess.Select(s => s.Id));

            foreach (var s in request.Seriess)
            {
                var author = authorToUpdate.Single(c => c.Id == s.Id);

                if (s.Monitored.HasValue)
                {
                    author.Monitored = s.Monitored.Value;
                }

                if (request.MonitoringOptions != null && request.MonitoringOptions.Monitor == MonitorTypes.None)
                {
                    author.Monitored = false;
                }

                if (request.MonitorNewItems.HasValue)
                {
                    author.MonitorNewItems = request.MonitorNewItems.Value;
                }

                _issueMonitoredService.SetIssueMonitoredStatus(author, request.MonitoringOptions);
            }

            return Accepted(request);
        }
    }
}
