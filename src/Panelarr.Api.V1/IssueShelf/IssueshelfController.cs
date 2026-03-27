using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Books;
using Panelarr.Http;

namespace Panelarr.Api.V1.Bookshelf
{
    [V1ApiController]
    public class IssueshelfController : Controller
    {
        private readonly ISeriesService _authorService;
        private readonly IBookMonitoredService _bookMonitoredService;

        public IssueshelfController(ISeriesService authorService, IBookMonitoredService bookMonitoredService)
        {
            _authorService = authorService;
            _bookMonitoredService = bookMonitoredService;
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

                _bookMonitoredService.SetBookMonitoredStatus(author, request.MonitoringOptions);
            }

            return Accepted(request);
        }
    }
}
