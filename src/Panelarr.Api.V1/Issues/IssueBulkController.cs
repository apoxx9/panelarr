using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Books;
using NzbDrone.Core.Messaging.Commands;
using Panelarr.Http;

namespace Panelarr.Api.V1.Books
{
    /// <summary>
    /// Dedicated bulk-operation endpoints for Issues.
    /// PUT  /api/v1/issue/monitor  — set monitored state on multiple issues
    /// DELETE /api/v1/issue/bulk   — delete multiple issues
    /// </summary>
    [V1ApiController("issue")]
    public class IssueBulkController : Controller
    {
        private readonly IBookService _bookService;
        private readonly IManageCommandQueue _commandQueueManager;

        public IssueBulkController(IBookService bookService, IManageCommandQueue commandQueueManager)
        {
            _bookService = bookService;
            _commandQueueManager = commandQueueManager;
        }

        /// <summary>PUT /api/v1/issue/monitor</summary>
        [HttpPut("monitor")]
        public IActionResult SetMonitored([FromBody] IssueMonitorResource resource)
        {
            _bookService.SetMonitored(resource.IssueIds, resource.Monitored);
            return Accepted();
        }

        /// <summary>DELETE /api/v1/issue/bulk</summary>
        [HttpDelete("bulk")]
        public IActionResult BulkDelete([FromBody] IssueBulkDeleteResource resource)
        {
            foreach (var issueId in resource.IssueIds)
            {
                _bookService.DeleteBook(issueId, resource.DeleteFiles);
            }

            return Ok();
        }
    }

    public class IssueMonitorResource
    {
        public List<int> IssueIds { get; set; }
        public bool Monitored { get; set; }
    }

    public class IssueBulkDeleteResource
    {
        public List<int> IssueIds { get; set; }
        public bool DeleteFiles { get; set; }
    }
}
