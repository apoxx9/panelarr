using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Issues;
using NzbDrone.Core.Messaging.Commands;
using Panelarr.Http;

namespace Panelarr.Api.V1.Issues
{
    [V1ApiController("issue/editor")]
    public class IssueEditorController : Controller
    {
        private readonly IIssueService _issueService;
        private readonly IManageCommandQueue _commandQueueManager;

        public IssueEditorController(IIssueService bookService, IManageCommandQueue commandQueueManager)
        {
            _issueService = bookService;
            _commandQueueManager = commandQueueManager;
        }

        [HttpPut]
        public IActionResult SaveAll([FromBody] IssueEditorResource resource)
        {
            var booksToUpdate = _issueService.GetIssues(resource.IssueIds);

            foreach (var issue in booksToUpdate)
            {
                if (resource.Monitored.HasValue)
                {
                    issue.Monitored = resource.Monitored.Value;
                }
            }

            _issueService.UpdateMany(booksToUpdate);
            return Accepted(booksToUpdate.ToResource());
        }

        [HttpDelete]
        public void DeleteIssue([FromBody] IssueEditorResource resource)
        {
            foreach (var issueId in resource.IssueIds)
            {
                _issueService.DeleteIssue(issueId, resource.DeleteFiles ?? false, resource.AddImportListExclusion ?? false);
            }
        }
    }
}
